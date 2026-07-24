// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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

        // ---------------------------------------------------------------------
        // OIDC FIC over mTLS (binding) E2E tests.
        //
        // Bound OIDC FIC flow (two token exchanges):
        //   the inner named app ("OidcFicIdp") authenticates with the automation certificate and
        //   mints an OIDC FIC assertion + binding certificate (audience api://AzureADTokenExchange);
        //   the outer app uses that assertion (ClientSignedAssertion) for the final leg.
        //
        // This is NOT the Managed Identity three-leg (MI -> inner OIDC -> outer); that composition is
        // covered at the provider level
        // (OidcIdpSignedAssertionProviderTests.GetSignedAssertionWithBindingAsync_InnerMsiFicResult_*).
        //
        // The binding certificate flows automatically from the inner acquisition result — a single
        // certificate credential is configured, never a duplicate binding credential.
        // ---------------------------------------------------------------------

        private const string LabTenant = "bea21ebe-8b64-4d06-9f6d-6a889b120a7c"; // MSI team tenant
        private const string LabClientId = "163ffef9-a313-45b4-ab2f-c7e2f5e0e23e";
        private const string AutomationCertificateSubject = "CN=LabAuth.MSIDLab.com";

        private static void ConfigureBoundOidcFic(
            TokenAcquirerFactory tokenAcquirerFactory,
            bool useBoundCredentialOnOuter)
        {
            // Inner (named) application: one certificate credential — no duplicate binding credential.
            // The lab automation certificate is provisioned in LocalMachine/My on the Azure DevOps
            // build agents (matching tests/E2E Tests/TokenAcquirerTests s_clientCredentials).
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(
                "OidcFicIdp",
                options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = LabTenant;
                    options.ClientId = LabClientId;
                    options.ClientCredentials = new[]
                    {
                        new CredentialDescription
                        {
                            SourceType = CredentialSource.StoreWithDistinguishedName,
                            CertificateStorePath = "LocalMachine/My",
                            CertificateDistinguishedName = AutomationCertificateSubject,
                        },
                    };
                });

            // Outer application: one CustomSignedAssertion credential. The binding certificate is
            // returned by the inner acquisition; no second certificate is configured.
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(
                options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = LabTenant;
                    options.ClientId = LabClientId;
                    options.ClientCapabilities = new[] { "cp1" };
                    options.ClientCredentials = new[]
                    {
                        new CredentialDescription
                        {
                            SourceType = CredentialSource.CustomSignedAssertion,
                            CustomSignedAssertionProviderName = "OidcIdpSignedAssertion",
                            CustomSignedAssertionProviderData = new Dictionary<string, object>
                            {
                                ["ConfigurationSection"] = "OidcFicIdp",
                            },
                            UseBoundCredential = useBoundCredentialOnOuter,
                        },
                    };
                });
        }

        private static string ComputeX5tS256(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
        {
            byte[] hash = SHA256.HashData(certificate.RawData);
            return Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        // E2E TEST A: final mtls_pop token. Requires the Microsoft identity automation test
        // certificate (CN=LabAuth.MSIDLab.com) and Azure DevOps lab connectivity.
        [OnlyOnAzureDevopsFact]
        public async Task OidcFic_FinalMtlsPopToken_BindsToInnerCertificate()
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddOidcFic();
            ConfigureBoundOidcFic(tokenAcquirerFactory, useBoundCredentialOnOuter: false);

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var headerProvider = (IAuthorizationHeaderProvider2)serviceProvider
                .GetRequiredService<IAuthorizationHeaderProvider>();

            var options = new AuthorizationHeaderProviderOptions
            {
                RequestAppToken = true,
                ProtocolScheme = "MTLS_POP",
            };

            // Act
            OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError> headerResult =
                await headerProvider.CreateAuthorizationHeaderInformationForAppAsync(
                    "https://graph.microsoft.com/.default",
                    options);

            // Assert
            Assert.True(headerResult.Succeeded);
            AuthorizationHeaderInformation info = headerResult.Result;

            // 1) mtls_pop scheme, 2) non-empty access token
            string headerValue = info.AuthorizationHeaderValue ?? string.Empty;
            Assert.StartsWith("MTLS_POP", headerValue, StringComparison.OrdinalIgnoreCase);
            string accessToken = headerValue["MTLS_POP ".Length..].Trim();
            Assert.False(string.IsNullOrEmpty(accessToken));

            // 3) non-null binding certificate
            Assert.NotNull(info.BindingCertificate);

            // 4) & 5) cnf.x5t#S256 matches SHA-256 thumbprint of the propagated binding certificate
            var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var cnfClaim = token.Claims.FirstOrDefault(c => c.Type == "cnf");
            Assert.NotNull(cnfClaim);

            using JsonDocument cnfDoc = JsonDocument.Parse(cnfClaim!.Value);
            string? x5tS256 = cnfDoc.RootElement.GetProperty("x5t#S256").GetString();
            Assert.False(string.IsNullOrEmpty(x5tS256));
            Assert.Equal(ComputeX5tS256(info.BindingCertificate!), x5tS256);

            // 6) token endpoint uses the mTLS auth endpoint
            Assert.Contains(
                "mtlsauth.microsoft.com",
                info.Metadata?.TokenEndpoint ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);

            // 7) client capability cp1 flows to the final token
            string[] xmsCc = token.Claims.Where(c => c.Type == "xms_cc").Select(c => c.Value).ToArray();
            Assert.Contains("cp1", xmsCc);
        }

        // E2E TEST B: final Bearer token with a bound OIDC credential (UseBoundCredential = true,
        // no ProtocolScheme = "MTLS_POP"). Mirrors the MSAL.NET
        // Sni_AssertionFlow_Uses_JwtPop_And_Acquires_Bearer_Token_TestAsync scenario: the client
        // assertion is jwt-pop over mTLS while the resulting access token is Bearer.
        [OnlyOnAzureDevopsFact]
        public async Task OidcFic_BoundCredential_FinalBearerToken_UsesMtlsClientAuthentication()
        {
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddOidcFic();
            ConfigureBoundOidcFic(tokenAcquirerFactory, useBoundCredentialOnOuter: true);

            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var headerProvider = (IAuthorizationHeaderProvider2)serviceProvider
                .GetRequiredService<IAuthorizationHeaderProvider>();

            // No ProtocolScheme = "MTLS_POP": the final token is Bearer, but client authentication
            // uses jwt-pop over mTLS because the outer OIDC credential is bound.
            var options = new AuthorizationHeaderProviderOptions
            {
                RequestAppToken = true,
            };

            // Act
            OperationResult<AuthorizationHeaderInformation, AuthorizationHeaderError> headerResult =
                await headerProvider.CreateAuthorizationHeaderInformationForAppAsync(
                    "https://graph.microsoft.com/.default",
                    options);

            // Assert
            Assert.True(headerResult.Succeeded);
            AuthorizationHeaderInformation info = headerResult.Result;

            // 1) Bearer scheme, 2) non-empty access token
            string headerValue = info.AuthorizationHeaderValue ?? string.Empty;
            Assert.StartsWith("Bearer", headerValue, StringComparison.OrdinalIgnoreCase);
            string accessToken = headerValue["Bearer ".Length..].Trim();
            Assert.False(string.IsNullOrEmpty(accessToken));

            // 3) token endpoint metadata shows the mTLS auth endpoint (jwt-pop client auth over mTLS).
            //    The exact client_assertion_type = jwt-pop wire assertion is validated in the mocked
            //    integration test; the live path asserts endpoint metadata + successful Bearer acquisition.
            Assert.Contains(
                "mtlsauth.microsoft.com",
                info.Metadata?.TokenEndpoint ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
