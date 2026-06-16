// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;


namespace CustomSignedAssertionProviderTests
{
    [CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    public class NonParallelCollection : ICollectionFixture<NonParallelFixture>
    {
        // This class has no code, and is never created
    }

    public class NonParallelFixture
    {
    }

    [Collection("Non-Parallel Collection")]
    public class OidCIdPSignedAssertionProviderExtensibilityTests
    {
        [OnlyOnAzureDevopsFact]
        public async Task CrossCloudFicIntegrationTest()
        {

            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddOidcFic();

            UpdateClientSecret(tokenAcquirerFactory); // for test only - get secret from KeyVault

            // this is how the authentication options can be configured in code rather than
            // in the appsettings file, though using the appsettings file is recommended
            /*            
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "msidlab4.onmicrosoft.com";
                options.ClientId = "5e71875b-ae52-4a3c-8b82-f6fdc8e1dbe1";
                options.ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "MyCustomExtension"
                }];
            });
            */
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default");

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("Bearer", result, StringComparison.Ordinal);

            // Decode token & verify xms_cc
            string jwt = result["Bearer ".Length..].Trim();

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            var xmsCcValues = token.Claims
                                    .Where(c => c.Type == "xms_cc")
                                    .Select(c => c.Value)
                                    .ToArray();

            Assert.Contains("cp1", xmsCcValues);
        }

        /// <summary>
        /// E2E for the OIDC FIC + mTLS Proof-of-Possession spike (issue #3851).
        ///
        /// Uses the MSI team's ESTS-allowlisted-for-mtls_pop app (163ffef9-...) in a
        /// self-FIC configuration: outer Entra app and inner OIDC IdP are the same app.
        /// The LabAuth cert (CN=LabAuth.MSIDLab.com) authenticates the inner leg AND
        /// is the binding key for the outer mTLS PoP token.
        ///
        /// Exercises:
        ///   1. The new bound-credential resolution path in OidcIdpSignedAssertionLoader
        ///      (UseBoundCredential = true detection + Skip mutation).
        ///   2. The new GetSignedAssertionWithBindingAsync path in OidcIdpSignedAssertionProvider
        ///      (SupportsTokenBinding override + ClientSignedAssertion bundle return).
        ///   3. The inner-leg PoP fix that mirrors IdWeb's "IsTokenBinding" magic string so the
        ///      inner ESTS call also requests mtls_pop (otherwise ESTS rejects with AADSTS392199).
        ///   4. The outer CCA's regional mTLS routing
        ///      (login.microsoftonline.com → westus3.mtlsauth.microsoft.com).
        ///
        /// Asserts that the returned header is a real mtls_pop token whose cnf claim's
        /// x5t#S256 thumbprint matches the LabAuth cert and whose xms_cc claim contains "cp1"
        /// (proving outer ClientCapabilities propagated through the FIC + PoP path).
        /// </summary>
        [OnlyOnAzureDevopsFact]
        public async Task OidcFic_WithMtlsPop_ReturnsBoundToken()
        {
            // Arrange
            const string Tenant = "bea21ebe-8b64-4d06-9f6d-6a889b120a7c";
            const string ClientId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
            const string Region = "westus3";
            const string LabAuthSubject = "CN=LabAuth.MSIDLab.com";

            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddOidcFic();

            // Outer: OIDC FIC CustomSignedAssertion that points at the "OidcFicIdp" inner section.
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = Tenant;
                options.ClientId = ClientId;
                options.AzureRegion = Region;
                options.ClientCapabilities = new[] { "cp1" };
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.CustomSignedAssertion,
                        CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                        CustomSignedAssertionProviderData = new Dictionary<string, object>
                        {
                            ["ConfigurationSection"] = "OidcFicIdp"
                        }
                    }
                };
            });

            // Inner (self-FIC): same app + LabAuth cert.
            //   Entry 1 (no UseBoundCredential)     : inner-leg auth to the external OIDC IdP.
            //   Entry 2 (UseBoundCredential = true) : binding cert resolved by the spike loader;
            //                                         marked Skip=true so the inner leg can't pick it.
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>("OidcFicIdp", options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = Tenant;
                options.ClientId = ClientId;
                options.AzureRegion = Region;
                options.SendX5C = true;
                options.ClientCredentials = new[]
                {
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.StoreWithDistinguishedName,
                        CertificateStorePath = "CurrentUser/My",
                        CertificateDistinguishedName = LabAuthSubject
                    },
                    new CredentialDescription
                    {
                        SourceType = CredentialSource.StoreWithDistinguishedName,
                        CertificateStorePath = "CurrentUser/My",
                        CertificateDistinguishedName = LabAuthSubject,
                        UseBoundCredential = true
                    }
                };
            });

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            IAuthorizationHeaderProvider authorizationHeaderProvider =
                serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act. On a cold process MSAL's region discovery (IMDS + user-provided region) can
            // race with the first request and the global endpoint can return a 307 to the
            // regional mTLS endpoint, which MSAL surfaces as non_parsable_oauth_error. Once
            // region discovery is cached every subsequent call hits the regional endpoint
            // directly, so a single retry is enough. (We can't warm up with a bearer call to
            // the same authority because IdWeb's outer CCA cache key does not include the
            // mTLS-PoP flag — a bearer warm-up would cache a string-assertion CCA that the
            // PoP call would then incorrectly reuse.)
            string header = await AcquirePopHeaderWithRetryAsync(
                authorizationHeaderProvider,
                "https://graph.microsoft.com/.default")
                .ConfigureAwait(false);

            // Assert: header is an mtls_pop token (MSAL emits the scheme name in lowercase).
            Assert.NotNull(header);
            Assert.StartsWith("mtls_pop ", header, StringComparison.OrdinalIgnoreCase);

            // Decode the JWT and verify it carries proof-of-possession claims.
            string jwt = header.Substring("mtls_pop ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            // xms_cc must include "cp1" — proves outer ClientCapabilities propagated through
            // the FIC + PoP path (would silently regress if the outer MergedOptions ever lost
            // ClientCapabilities for the binding-CCA build).
            string[] xmsCcValues = token.Claims
                .Where(c => c.Type == "xms_cc")
                .Select(c => c.Value)
                .ToArray();
            Assert.Contains("cp1", xmsCcValues);

            // cnf must contain x5t#S256 — proves the outer token is actually bound to a
            // certificate (the central security property of mTLS PoP).
            var cnfClaim = token.Claims.FirstOrDefault(c => c.Type == "cnf");
            Assert.NotNull(cnfClaim);

            using JsonDocument cnfDoc = JsonDocument.Parse(cnfClaim!.Value);
            string? cnfThumbprint = cnfDoc.RootElement.GetProperty("x5t#S256").GetString();
            Assert.False(string.IsNullOrEmpty(cnfThumbprint));

            // x5t#S256 must equal base64url(SHA256(LabAuth cert DER)) — proves the token
            // is bound to *our* configured binding cert specifically, not some other cert
            // that MSAL might have picked up. This is the strongest available signal that
            // the spike's binding-cert resolution + Skip wiring is working end-to-end.
            // The lab agent typically has multiple non-expired LabAuth certs in the store
            // (overlap during rotation), so we accept a match against any of them.
            var labAuthThumbprints = new HashSet<string>(StringComparer.Ordinal);
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName,
                    LabAuthSubject,
                    validOnly: false);
                Assert.True(
                    matches.Count > 0,
                    $"LabAuth cert ({LabAuthSubject}) not found in CurrentUser/My store.");

                foreach (var cert in matches)
                {
                    byte[] hash = SHA256.HashData(cert.RawData);
                    string base64url = Convert.ToBase64String(hash)
                        .TrimEnd('=')
                        .Replace('+', '-')
                        .Replace('/', '_');
                    labAuthThumbprints.Add(base64url);
                }
            }
            Assert.True(
                labAuthThumbprints.Contains(cnfThumbprint!),
                $"Token cnf x5t#S256 '{cnfThumbprint}' does not match any LabAuth cert in CurrentUser/My. " +
                $"Expected one of: {string.Join(", ", labAuthThumbprints)}");
        }

        private static void UpdateClientSecret(TokenAcquirerFactory tokenAcquirerFactory)
        {
            KeyVaultSecretsProvider ksp = new KeyVaultSecretsProvider();
            var secret = ksp.GetSecretByName("ARLMSIDLAB1-IDLASBS-App-CC-Secret").Value;


            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            tokenAcquirerFactory.Services.AddSingleton<IConfiguration>(configuration);

            // Bind the AzureAd2 section into the named options.
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(
                "AzureAd2",
                configuration.GetSection("AzureAd2"));

            // Apply any dynamic overrides after the JSON bind.
            tokenAcquirerFactory.Services.PostConfigure<MicrosoftIdentityApplicationOptions>(
                "AzureAd2",
                options =>
                {
                    options.ClientCredentials = new[]
                    {
                        new CredentialDescription
                        {
                            SourceType = CredentialSource.ClientSecret,
                            ClientSecret = secret
                        }
                    };

                });
        }

        /// <summary>
        /// Single-shot retry for the cold-process race between MSAL's region discovery and
        /// the first PoP request. On a cold process the first call can hit the global endpoint
        /// before regional discovery is cached; ESTS responds with 307 to the regional mTLS
        /// endpoint and MSAL surfaces this as non_parsable_oauth_error. Once the region cache
        /// is warmed the retry succeeds.
        /// </summary>
        private static async Task<string> AcquirePopHeaderWithRetryAsync(
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            string scope)
        {
            var popOptions = new AuthorizationHeaderProviderOptions { ProtocolScheme = "MTLS_POP" };
            try
            {
                return await authorizationHeaderProvider
                    .CreateAuthorizationHeaderForAppAsync(scope, popOptions)
                    .ConfigureAwait(false);
            }
            catch (Microsoft.Identity.Client.MsalServiceException ex) when (
                ex.StatusCode == 307 ||
                string.Equals(ex.ErrorCode, "non_parsable_oauth_error", StringComparison.OrdinalIgnoreCase))
            {
                return await authorizationHeaderProvider
                    .CreateAuthorizationHeaderForAppAsync(scope, popOptions)
                    .ConfigureAwait(false);
            }
        }

        //[Fact(Skip ="Does not run if run with the E2E test")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CrossCloudFicUnitTest(bool withFmiPath)
        {
            // Arrange
            using (MockHttpClientFactory httpFactoryForTest = new MockHttpClientFactory())
            {
                var credentialRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                    MockHttpCreator.CreateClientCredentialTokenHandler());
                var tokenRequestHttpHandler = httpFactoryForTest.AddMockHandler(
                    MockHttpCreator.CreateClientCredentialTokenHandler());

                TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
                TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
                tokenAcquirerFactory.Services.AddOidcFic();
                tokenAcquirerFactory.Services.AddSingleton<IHttpClientFactory>(httpFactoryForTest);

                tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>("AzureAd2",
                       options =>
                       {
                           options.Instance = "https://login.microsoftonline.us/";
                           options.TenantId = "t1";
                           options.ClientId = "c1";
                           options.ClientCredentials = [ new CredentialDescription() {
                            SourceType = CredentialSource.ClientSecret,
                            ClientSecret = TestConstants.ClientSecret
                        }];
                       });

                Dictionary<string, object> customAssertionProvidedData = new()
                {
                    ["ConfigurationSection"] = "AzureAd2"
                };
                if (withFmiPath)
                {
                    customAssertionProvidedData.Add("RequiresSignedAssertionFmiPath", true);
                }

                tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = "t2";
                    options.ClientId = "c2";
                    options.ClientCapabilities = ["cp1"];
                    options.ExtraQueryParameters = null;
                    options.ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                    CustomSignedAssertionProviderData = customAssertionProvidedData
                    }];
                });

                IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
                IAuthorizationHeaderProvider authorizationHeaderProvider =
                    serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();


                AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions;
                if (withFmiPath)
                {
                    authorizationHeaderProviderOptions = new()
                    {
                        AcquireTokenOptions = new()
                        {
                            ExtraParameters = new Dictionary<string, object>()
                            {
                                [Constants.FmiPathForClientAssertion] = "myFmiPathForSignedAssertion"
                            }
                        }
                    };
                }
                else
                {
                    authorizationHeaderProviderOptions = null;
                }

                // Act
                var result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(TestConstants.s_scopeForApp,
                    authorizationHeaderProviderOptions);

                // Assert
                Assert.Equal("api://AzureADTokenExchange/.default", credentialRequestHttpHandler.ActualRequestPostData["scope"]);
                Assert.Equal(TestConstants.s_scopeForApp, tokenRequestHttpHandler.ActualRequestPostData["scope"]);
                Assert.Equal("c1", credentialRequestHttpHandler.ActualRequestPostData["client_id"]);
                Assert.Equal("https://login.microsoftonline.us/t1/oauth2/v2.0/token", credentialRequestHttpHandler.ActualRequestMessage?.RequestUri?.AbsoluteUri);
                Assert.Equal("c2", tokenRequestHttpHandler.ActualRequestPostData["client_id"]);
                Assert.Equal("https://login.microsoftonline.com/t2/oauth2/v2.0/token", tokenRequestHttpHandler.ActualRequestMessage?.RequestUri?.AbsoluteUri);

                if (withFmiPath)
                {
                    Assert.Equal("myFmiPathForSignedAssertion", credentialRequestHttpHandler.ActualRequestPostData["fmi_path"]);
                }

                // First request (credential exchange) – should have *no* "claims"
                Assert.False(credentialRequestHttpHandler.ActualRequestPostData
                                                             .ContainsKey("claims"));

                // Second request (real token acquisition) – must carry "claims"
                Assert.True(tokenRequestHttpHandler.ActualRequestPostData
                                                   .ContainsKey("claims"));

                // Extract and inspect the JSON payload
                string claimsJson = tokenRequestHttpHandler.ActualRequestPostData["claims"];

                using JsonDocument doc = JsonDocument.Parse(claimsJson);

                string? cp = doc.RootElement
                .GetProperty("access_token")
                .GetProperty("xms_cc")
                .GetProperty("values")[0]
                .GetString();

                // Ensure that the client capabilities are passed in the claims
                Assert.Equal("cp1", cp);

                string? accessTokenFromRequest1;
                using (JsonDocument document = JsonDocument.Parse(credentialRequestHttpHandler.ResponseString))
                {
                    accessTokenFromRequest1 = document.RootElement.GetProperty("access_token").GetString();
                }

                // the jwt credential from request1 is used as credential on request2
                Assert.Equal(
                    tokenRequestHttpHandler.ActualRequestPostData["client_assertion"],
                    accessTokenFromRequest1);
            }
        }
    }
}
