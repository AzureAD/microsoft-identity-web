// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Experimental;
using Xunit;

namespace TokenAcquirerTests
{
    public sealed class CertificateRotationTest : ICertificatesObserver
    {
        const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
        const string TenantId = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab";
        const double ValidityFirstCertInMinutes = 1.5;
        const double ValiditySecondCertInMinutes = 10;
        ServicePrincipal? _servicePrincipal;
        readonly GraphServiceClient _graphServiceClient;
        X509Certificate? _currentCertificate = null;

        public CertificateRotationTest()
        {
            // Instantiate a Graph client
            DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions()
            {
                VisualStudioTenantId = TenantId,
            });
            _graphServiceClient = new GraphServiceClient(credential);
        }

        [IgnoreOnAzureDevopsFact]
        public async Task TestCertificateRotationAsync()
        {
            // Prepare the environment
            // -----------------------
            // Create an app registration for a daemon app
            Application aadApplication = (await CreateDaemonAppRegistrationIfNeededAsync())!;
            DateTimeOffset now = DateTimeOffset.Now;

            // Create a certificate expiring in 3 mins, add it to the local cert store
            X509Certificate2 firstCertificate = CreateSelfSignedCertificateAddAddToCertStore(
                "MySelfSignedCert",
                now.AddMinutes(ValidityFirstCertInMinutes));

            // Create a cert active in 2 mins, and expiring in 10 mins, and add it to the cert store
            X509Certificate2 secondCertificate = CreateSelfSignedCertificateAddAddToCertStore(
                "MySelfSignedCert",
                 now.AddMinutes(ValiditySecondCertInMinutes),
                 now.AddMinutes(ValidityFirstCertInMinutes - 0.25));

            // and add it as client creds to the app registration
            await AddClientCertificatesToAppAsync(aadApplication!, firstCertificate, secondCertificate);

            // Code for the application
            // ------------------------
            // Add the cert to the configuration
            CredentialDescription[] clientCertificates = new CredentialDescription[]
            {
                new CertificateDescription
                {
                     CertificateDistinguishedName = firstCertificate.SubjectName.Name,
                     SourceType = CertificateSource.StoreWithDistinguishedName,
                     CertificateStorePath = "CurrentUser/My",
                }
            };

            // Use the token acquirer factory to run the app and acquire a token
            TokenAcquirerFactory.ResetDefaultInstance();
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = $"https://login.microsoftonline.com/";
                options.ClientId = aadApplication!.AppId;
                options.TenantId = TenantId;
                options.ClientCredentials = clientCertificates;
            });
            tokenAcquirerFactory.Services.AddSingleton<ICertificatesObserver>(this);
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Before acquiring a token, wait so that the certificate is considered in the app-registration
            // (this is not immediate :-()
            await Task.Delay(TimeSpan.FromSeconds(30));

            string authorizationHeader;
            try
            {
                authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                "https://graph.microsoft.com/.default");
                Assert.NotNull(authorizationHeader);
                Assert.NotEqual(string.Empty, authorizationHeader);
            }
            catch (Exception)
            {
                await RemoveAppAndCertificatesAsync(firstCertificate);
                Assert.Fail("Failed to acquire token with the first certificate");
            }
            finally
            {
            }

            // Keep acquiring tokens every minute for 5 mins
            // Tokens should be acquired successfully
            for (int i = 0; i < 6; i++)
            {
                // Wait for a minute
                await Task.Delay(60 * 1000);

                // Acquire a token
                try
                {
                    authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                                           "https://graph.microsoft.com/.default",
                                           new AuthorizationHeaderProviderOptions()
                                           {
                                               AcquireTokenOptions = new AcquireTokenOptions
                                               {
                                                   ForceRefresh = true // Exceptionnaly as we want to test the cert rotation.
                                               }
                                           });
                    Assert.NotNull(authorizationHeader);
                    Assert.NotEqual(string.Empty, authorizationHeader);

                    // If the token acquisition was successful and the cert used is the second one, the test can terminate.
                    if (_currentCertificate != null && _currentCertificate.GetPublicKeyString() == secondCertificate.GetPublicKeyString())
                    {
                        break;
                    }

                }
                catch (Exception)
                {
                    await RemoveAppAndCertificatesAsync(firstCertificate, secondCertificate);
                    Assert.Fail("Failed to acquire token with the second certificate");
                }
            }

            // Check the last certificate used is the second one.
            Assert.True(_currentCertificate != null && _currentCertificate.GetPublicKeyString() == secondCertificate.GetPublicKeyString());

            // Delete both certs from the cert store and remove the app registration
            await RemoveAppAndCertificatesAsync(firstCertificate, secondCertificate);
        }

        private async Task RemoveAppAndCertificatesAsync(
            X509Certificate2 firstCertificate,
            X509Certificate2? secondCertificate = null,
            Application? application = null,
            ServicePrincipal? servicePrincipal = null)
        {
            // Delete the cert from the cert store
            X509Store x509Store = new(StoreName.My, StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadWrite);
            x509Store.Remove(firstCertificate);
            if (secondCertificate != null)
            {
                x509Store.Remove(secondCertificate);
            }
            x509Store.Close();

            // Delete the app registration
            if (application != null)
            {
                await _graphServiceClient.Applications[$"{application!.Id}"]
                    .DeleteAsync();
            }
            if (servicePrincipal != null)
            {
                await _graphServiceClient.ServicePrincipals[$"{_servicePrincipal!.Id}"]
                    .DeleteAsync();
            }
        }

        private async Task<Application?> CreateDaemonAppRegistrationIfNeededAsync()
        {
            var application = (await _graphServiceClient
                .Applications
                .GetAsync(options => options.QueryParameters.Filter = $"DisplayName eq 'Daemon app to test cert rotation'"))
                ?.Value?.FirstOrDefault();

            if (application == null)
            {
                application = await CreateDaemonAppRegistrationAsync();
            }
            return application!;
        }

        private async Task<Application?> CreateDaemonAppRegistrationAsync()
        {
            // Get the Microsoft Graph service principal and the user.read.all role.
            ServicePrincipal graphSp = (await _graphServiceClient.ServicePrincipals
                .GetAsync(options => options.QueryParameters.Filter = $"AppId eq '{MicrosoftGraphAppId}'"))!.Value!.First();
            AppRole userReadAllRole = graphSp!.AppRoles!.First(r => r.Value == "User.Read.All");

            // Create an app with API permissions to user.read.all
            Application application = new()
            {
                DisplayName = "Daemon app to test cert rotation",
                SignInAudience = "AzureADMyOrg",
                Description = "Daemon to test cert rotation",
                RequiredResourceAccess = new System.Collections.Generic.List<RequiredResourceAccess>
                    {
                        new()
                        {
                            ResourceAppId = MicrosoftGraphAppId,
                            ResourceAccess = new System.Collections.Generic.List<ResourceAccess>()
                         {
                             new()
                             {
                                 Id = userReadAllRole.Id,
                                 Type = "Role",
                             }
                         }
                        }
                }
            };
            Application createdApp = (await _graphServiceClient.Applications
                .PostAsync(application))!;

            // Create a service principal for the app
            var servicePrincipal = new ServicePrincipal
            {
                AppId = createdApp!.AppId,
            };
            _servicePrincipal = await _graphServiceClient.ServicePrincipals
                .PostAsync(servicePrincipal).ConfigureAwait(false);

            // Grant admin consent to user.read.all
            var oAuth2PermissionGrant = new OAuth2PermissionGrant
            {
                ClientId = _servicePrincipal!.Id,
                ConsentType = "AllPrincipals",
                PrincipalId = null,
                ResourceId = graphSp.Id,
                Scope = userReadAllRole.Value,
            };

            try
            {
                var effectivePermissionGrant = await _graphServiceClient.Oauth2PermissionGrants
                    .PostAsync(oAuth2PermissionGrant);
            }
            catch (Exception)
            {
            }

            return createdApp;
        }

        private X509Certificate2 CreateSelfSignedCertificateAddAddToCertStore(string certName, DateTimeOffset expiry, DateTimeOffset? notBefore = null)
        {
            // Create the self signed certificate
#if ECDsa
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={certName}", ecdsa, HashAlgorithmName.SHA256);
#else
            using RSA rsa = RSA.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={certName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif

            var cert = req.CreateSelfSigned(notBefore.HasValue ? notBefore.Value : DateTimeOffset.Now, expiry);

            byte[] bytes = cert.Export(X509ContentType.Pfx, (string?)null);
            X509Certificate2 certWithPrivateKey = new(bytes);

            // Add it to the local cert store.
            X509Store x509Store = new(StoreName.My, StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadWrite);
            x509Store.Add(certWithPrivateKey);
            x509Store.Close();
            return certWithPrivateKey;
        }

        private async Task<Application> AddClientCertificatesToAppAsync(Application application, X509Certificate2 firstCertificate, X509Certificate2 secondCertificate2)
        {
            Application update = new()
            {
                KeyCredentials = new System.Collections.Generic.List<KeyCredential>()
                {
                         new()
                         {
                             DisplayName = GetDisplayName(firstCertificate),
                             EndDateTime = firstCertificate.NotAfter,
                             StartDateTime = firstCertificate.NotBefore,
                             Type = "AsymmetricX509Cert",
                             Usage = "Verify",
                             Key = firstCertificate.Export(X509ContentType.Cert)
                         },
                        new()
                        {
                            DisplayName = GetDisplayName(secondCertificate2),
                            EndDateTime = secondCertificate2.NotAfter,
                            StartDateTime = secondCertificate2.NotBefore,
                            Type = "AsymmetricX509Cert",
                            Usage = "Verify",
                            Key = secondCertificate2.Export(X509ContentType.Cert)
                        }
                  }
            };
            return (await _graphServiceClient.Applications[application.Id].PatchAsync(update))!;
        }

        private static string GetDisplayName(X509Certificate2 cert)
        {
            return cert.NotBefore.ToString(CultureInfo.InvariantCulture)
                + "-"
                + cert.NotAfter.ToString(CultureInfo.InvariantCulture);
        }

        void ICertificatesObserver.OnClientCertificateChanged(CertificateChangeEventArg e)
        {
            switch (e.Action)
            {
                case CerticateObserverAction.Selected:
                    _currentCertificate = e.Certificate;
                    break;

                case CerticateObserverAction.Deselected:
                    _currentCertificate = null;
                    break;
            }
        }
    }
}
