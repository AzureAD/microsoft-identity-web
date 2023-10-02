// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test.Integration
{
    public class CertificateRotationTest
    {
        const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";
        const string tenantId = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab";
   //     Application? _application;
        ServicePrincipal? _servicePrincipal;
        GraphServiceClient graphServiceClient;


        public CertificateRotationTest()
        {
            // Instantiate a Graph client
            DefaultAzureCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                VisualStudioTenantId = tenantId,
            });
            graphServiceClient = new GraphServiceClient(credential);
        }

        [Fact]
        public async Task TestCertificateRotation()
        {
            // Create an app registration for a daemon app
            Application aadApplication = await CreateDaemonAppRegistrationIfNeeded();

            // Create a certificate expiring in 3 mins, add it to the local cert store
            X509Certificate2 firstCertificate = CreateSelfSignedCertificateAddAddToCertStore(
                "MySelfSignedCert",
                DateTimeOffset.Now.AddMinutes(3));

            // and add it as client creds
            await AddClientCertificateToApp(aadApplication!, firstCertificate);

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
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = $"https://login.microsoftonline.com/";
                options.ClientId = aadApplication!.AppId;
                options.TenantId = tenantId;
                options.ClientCredentials = clientCertificates;
            });
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
            }
            catch (Exception ex)
            {
                await RemoveAppAndCertificates(firstCertificate);
                Assert.Fail("Failed to acquire token with the first certificate");
            }
            finally
            {
            }

            // Create a new certificate with the same distinguish name, expiring later and
            // add it to the store
            X509Certificate2 secondCertificate = CreateSelfSignedCertificateAddAddToCertStore(
                "MySelfSignedCert",
                DateTimeOffset.Now.AddMinutes(10));

            // add this certificate as client creds to the app registration.
            // You would have to do that except if you have an SN/I cert and use UseX5C in the config.
            await AddClientCertificateToApp(aadApplication, secondCertificate);

            // Keep acquiring tokens every minute for 5 mins
            // Tokens should be acquired successfully
            for (int i = 0; i < 5; i++)
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
                }
                catch (Exception ex)
                {
                    await RemoveAppAndCertificates(firstCertificate, secondCertificate);
                    Assert.Fail("Failed to acquire token with the second certificate");
                }
            }   


            // Delete both certs from the cert store and remove the app registration
            await RemoveAppAndCertificates(firstCertificate, secondCertificate);
        }

        private async Task RemoveAppAndCertificates(
            X509Certificate2 firstCertificate,
            X509Certificate2? secondCertificate = null, 
            Application? application = null, 
            ServicePrincipal? servicePrincipal = null)
        {
            // Delete the cert from the cert store
            X509Store x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadWrite);
            x509Store.Remove(firstCertificate);
            if (secondCertificate !=null)
            {
                x509Store.Remove(secondCertificate);
            }
            x509Store.Close();

            // Delete the app registration
            if (application != null)
            {
                await graphServiceClient.Applications[$"{application!.Id}"]
                    .DeleteAsync();
            }
            if (servicePrincipal != null)
            { 
                await graphServiceClient.ServicePrincipals[$"{_servicePrincipal!.Id}"]
                    .DeleteAsync();
            }
        }


        private async Task<Application?> CreateDaemonAppRegistrationIfNeeded()
        {
            var application = (await graphServiceClient
                .Applications
                .GetAsync(options => options.QueryParameters.Filter = $"DisplayName eq 'Daemon app to test cert rotation'"))
                ?.Value?.FirstOrDefault();
                
            if (application == null)
            {
                application = await CreateDaemonAppRegistration();
            }
            return application!;
        }

         private async Task<Application?> CreateDaemonAppRegistration()
        {
            // Get the Microsoft Graph service principal and the user.read.all role.
            ServicePrincipal graphSp = (await graphServiceClient.ServicePrincipals
                .GetAsync(options => options.QueryParameters.Filter = $"AppId eq '{MicrosoftGraphAppId}'"))!.Value!.First();
            AppRole userReadAllRole = graphSp!.AppRoles!.First(r => r.Value == "User.Read.All");

            // Create an app with API permissions to user.read.all
            Application application = new Application()
            {
                DisplayName = "Daemon app to test cert rotation",
                SignInAudience = "AzureADMyOrg",
                Description = "Daemon to test cert rotation",
                RequiredResourceAccess = new System.Collections.Generic.List<RequiredResourceAccess>
                    {
                        new RequiredResourceAccess()
                        {
                            ResourceAppId = MicrosoftGraphAppId,
                            ResourceAccess = new System.Collections.Generic.List<ResourceAccess>()
                         {
                             new ResourceAccess()
                             {
                                 Id = userReadAllRole.Id,
                                 Type = "Role",
                             }
                         }
                        }
                }
            };
            Application createdApp = await graphServiceClient.Applications
                .PostAsync(application)!;

            // Create a service principal for the app
            var servicePrincipal = new ServicePrincipal
            {
                AppId = createdApp!.AppId,
            };
            _servicePrincipal = await graphServiceClient.ServicePrincipals
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
                var effectivePermissionGrant = await graphServiceClient.Oauth2PermissionGrants
                    .PostAsync(oAuth2PermissionGrant);
            }
            catch (Exception ex)
            {
            }

            return createdApp;
        }

        private X509Certificate2 CreateSelfSignedCertificateAddAddToCertStore(string certName, DateTimeOffset expiry)
        {
            // Create the self signed certificate
#if ECDsa
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={certName}", ecdsa, HashAlgorithmName.SHA256);
#else
            using RSA rsa = RSA.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={certName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#endif

            var cert = req.CreateSelfSigned(DateTimeOffset.Now, expiry);

            byte[] bytes = cert.Export(X509ContentType.Pfx, (string?)null);
            X509Certificate2 certWithPrivateKey = new X509Certificate2(bytes);

            // Add it to the local cert store.
            X509Store x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadWrite);
            x509Store.Add(certWithPrivateKey);
            x509Store.Close();
            return certWithPrivateKey;
        }

        private async Task<Application> AddClientCertificateToApp(Application application, X509Certificate2 firstCertificate)
        {
            KeyCredential keyCredential = new KeyCredential()
            {
                EndDateTime = firstCertificate.NotAfter,
                StartDateTime = firstCertificate.NotBefore,
             //   KeyId = Guid.NewGuid(),
                Type = "AsymmetricX509Cert",
                Usage = "Verify",
                Key = firstCertificate.Export(X509ContentType.Cert)
            };
            application.KeyCredentials.Clear();
            application.KeyCredentials.Add(keyCredential);
            return await graphServiceClient.Applications[application.Id].PatchAsync(application);
        }
    }
}
