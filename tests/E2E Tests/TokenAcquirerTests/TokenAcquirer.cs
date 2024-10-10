// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace TokenAcquirerTests
{
#if !FROM_GITHUB_ACTION
    public class TokenAcquirer
    {
        private static readonly string s_optionName = string.Empty;
        private static readonly CredentialDescription[] s_clientCredentials = new[]
        {
            CertificateDescription.FromKeyVault(
                "https://webappsapistests.vault.azure.net",
                "Self-Signed-5-5-22")
        };

        private static readonly CredentialDescription[] s_ciamClientCredentials = new[]
        {
            CertificateDescription.FromKeyVault(
                "https://buildautomation.vault.azure.net",
                "AzureADIdentityDivisionTestAgentCert")
        };

        public TokenAcquirer()
        {
            TokenAcquirerFactory.ResetDefaultInstance(); // Test only
        }

        [Fact]
        public void TokenAcquirerFactoryDoesNotUseAspNetCoreHost()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            var serviceProvider = tokenAcquirerFactory.Build();
            var service = serviceProvider.GetService<ITokenAcquisitionHost>();
            Assert.NotNull(service);
            Assert.Equal("Microsoft.Identity.Web.Hosts.DefaultTokenAcquisitionHost", service.GetType().FullName);
        }

        [Fact]
        public void DefaultTokenAcquirer_GetKeyHandlesNulls()
        {
            var res = DefaultTokenAcquirerFactoryImplementation.GetKey("1", "2", "3");
            Assert.Equal("123", res);

            var no_region = DefaultTokenAcquirerFactoryImplementation.GetKey("1", "2", null);
            Assert.Equal("12", no_region);
        }

        [Fact]
        public void AcquireToken_WithMultipleRegions()
        {
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            _ = tokenAcquirerFactory.Build();

            ITokenAcquirer tokenAcquirerA = tokenAcquirerFactory.GetTokenAcquirer(
               authority: "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
               clientId: "6af093f3-b445-4b7a-beae-046864468ad6",
               clientCredentials: s_clientCredentials,
               "US");

            ITokenAcquirer tokenAcquirerB = tokenAcquirerFactory.GetTokenAcquirer(
               authority: "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
               clientId: "6af093f3-b445-4b7a-beae-046864468ad6",
               clientCredentials: s_clientCredentials,
               "US");

            ITokenAcquirer tokenAcquirerC = tokenAcquirerFactory.GetTokenAcquirer(
               authority: "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
               clientId: "6af093f3-b445-4b7a-beae-046864468ad6",
               clientCredentials: s_clientCredentials,
               "EU");

            Assert.Equal(tokenAcquirerA, tokenAcquirerB);
            Assert.NotEqual(tokenAcquirerA, tokenAcquirerC);
        }

        [Fact]
        public void AcquireToken_SafeFromMultipleThreads()
        {
            var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            _ = tokenAcquirerFactory.Build();

            var count = new ConcurrentDictionary<ITokenAcquirer, bool>();

            var action = () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    ITokenAcquirer res = tokenAcquirerFactory.GetTokenAcquirer(
                      authority: "https://login.microsoftonline.com/msidentitysamplestesting.onmicrosoft.com",
                      clientId: "6af093f3-b445-4b7a-beae-046864468ad6",
                      clientCredentials: s_clientCredentials,
                      "" + (i%11));

                    count.TryAdd(res, true);
                }
            };

            Thread[] threads = new Thread[16];
            for (int i = 0; i < 16; i++)
            {
                threads[i] = new Thread(() => action());
                threads[i].Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            Assert.Equal(11, count.Count);
        }

        [IgnoreOnAzureDevopsFact]
        //[Theory]
        //[InlineData(false)]
        //[InlineData(true)]
        public async Task AcquireToken_WithMicrosoftIdentityOptions_ClientCredentialsAsync(/*bool withClientCredentials*/)
        {
            bool withClientCredentials = false; //add as param above
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidlab4.onmicrosoft.com";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                if (withClientCredentials)
                {
                    option.ClientCertificates = s_clientCredentials.OfType<CertificateDescription>();
                }
                else
                {
                    option.ClientCredentials = s_clientCredentials;
                }
            });

            await CreateGraphClientAndAssertAsync(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireToken_WithMicrosoftIdentityApplicationOptions_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidlab4.onmicrosoft.com";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            await CreateGraphClientAndAssertAsync(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact(Skip = "https://github.com/AzureAD/microsoft-identity-web/issues/2732")]
        //[Fact]
        public async Task AcquireToken_WithMicrosoftIdentityApplicationOptions_ClientCredentialsCiamAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Authority = "https://MSIDLABCIAM6.ciamlogin.com";
                option.ClientId = "b244c86f-ed88-45bf-abda-6b37aa482c79";
                option.ClientCredentials = s_clientCredentials;
            });

            await CreateGraphClientAndAssertAsync(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        // [Fact]
        public async Task AcquireToken_WithFactoryAndMicrosoftIdentityApplicationOptions_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddInMemoryTokenCaches();
            tokenAcquirerFactory.Build();

            // Get the token acquirer from the options.
            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(new MicrosoftIdentityApplicationOptions
            {
                ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba",
                Authority = "https://login.microsoftonline.com/msidlab4.onmicrosoft.com",
                ClientCredentials = s_clientCredentials
            });

            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
        }

        [IgnoreOnAzureDevopsFact]
        // [Fact]
        public async Task AcquireToken_WithFactoryAndAuthorityClientIdCert_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.AddInMemoryTokenCaches();
            tokenAcquirerFactory.Build();

            var tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(
                authority: "https://login.microsoftonline.com/msidlab4.onmicrosoft.com",
                clientId: "f6b698c0-140c-448f-8155-4aa9bf77ceba",
                clientCredentials: s_clientCredentials);

            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task LoadCredentialsIfNeededAsync_MultipleThreads_WaitsForSemaphoreAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidlab4.onmicrosoft.com";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            services.AddInMemoryTokenCaches();
            var serviceProvider = tokenAcquirerFactory.Build();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(s_optionName);
            var credentialsLoader = serviceProvider.GetRequiredService<ICredentialsLoader>();

            var task1 = Task.Run(async () =>
            {
                await credentialsLoader.LoadCredentialsIfNeededAsync(options.ClientCredentials!.First());
            });

            var task2 = Task.Run(async () =>
            {
                await credentialsLoader.LoadCredentialsIfNeededAsync(options.ClientCredentials!.First());
            });

            // Run task1 and task2 concurrently
            await Task.WhenAll(task1, task2);

            var cert = options.ClientCredentials!.First().Certificate;

            Assert.NotNull(cert);
            Assert.Equal(TaskStatus.RanToCompletion, task1.Status);
            Assert.Equal(TaskStatus.RanToCompletion, task2.Status);
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireTokenWithPop_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidlab4.onmicrosoft.com";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            services.AddInMemoryTokenCaches();
            var serviceProvider = tokenAcquirerFactory.Build();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(s_optionName);
            var credentialsLoader = serviceProvider.GetRequiredService<ICredentialsLoader>();
            await credentialsLoader.LoadCredentialsIfNeededAsync(options.ClientCredentials!.First());
            var cert = options.ClientCredentials!.First().Certificate;

            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(s_optionName);
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default",
                   new TokenAcquisitionOptions() { PopPublicKey = ComputePublicKeyString(cert) });
            Assert.NotNull(result.AccessToken);
        }

        [IgnoreOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireTokenWithMs10AtPop_ClientCredentialsAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidlab4.onmicrosoft.com";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            services.AddInMemoryTokenCaches();
            var serviceProvider = tokenAcquirerFactory.Build();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(s_optionName);
            var credentialsLoader = serviceProvider.GetRequiredService<ICredentialsLoader>();
            await credentialsLoader.LoadCredentialsIfNeededAsync(options.ClientCredentials!.First());
            var cert = options.ClientCredentials!.First().Certificate;

            // Get the token acquisition service
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(s_optionName);
            RsaSecurityKey rsaSecurityKey = CreateRsaSecurityKey();
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default",
                   new TokenAcquisitionOptions()
                   {
                       PopPublicKey = rsaSecurityKey.KeyId,
                       PopClaim = CreatePopClaim(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
                   });
            Assert.NotNull(result.AccessToken);
        }

        private static string CreatePopClaim(RsaSecurityKey key, string algorithm)
        {
            var parameters = key.Rsa == null ? key.Parameters : key.Rsa.ExportParameters(false);
            return "{\"kty\":\"RSA\",\"n\":\"" + Base64UrlEncoder.Encode(parameters.Modulus) + "\",\"e\":\"" + Base64UrlEncoder.Encode(parameters.Exponent) + "\",\"alg\":\"" + algorithm + "\",\"kid\":\"" + key.KeyId + "\"}";
        }

        private static RsaSecurityKey CreateRsaSecurityKey()
        {
#if NET472
            RSA rsa = RSA.Create(2048);
#else
            RSA rsa = new RSACryptoServiceProvider(2048);
#endif
            // the reason for creating the RsaSecurityKey from RSAParameters is so that a SignatureProvider created with this key
            // will own the RSA object and dispose it. If we pass a RSA object, the SignatureProvider does not own the object, the RSA object will not be disposed.
            RSAParameters rsaParameters = rsa.ExportParameters(true);
            RsaSecurityKey rsaSecuirtyKey = new(rsaParameters) { KeyId = CreateRsaKeyId(rsaParameters) };
            rsa.Dispose();
            return rsaSecuirtyKey;
        }

        private static string CreateRsaKeyId(RSAParameters rsaParameters)
        {
            Throws.IfNull(rsaParameters.Exponent);
            Throws.IfNull(rsaParameters.Modulus);

            byte[] kidBytes = new byte[rsaParameters.Exponent.Length + rsaParameters.Modulus.Length];
            Array.Copy(rsaParameters.Exponent, 0, kidBytes, 0, rsaParameters.Exponent.Length);
            Array.Copy(rsaParameters.Modulus, 0, kidBytes, rsaParameters.Exponent.Length, rsaParameters.Modulus.Length);
            using (var sha2 = SHA256.Create())
            {
                return Base64UrlEncoder.Encode(sha2.ComputeHash(kidBytes));
            }
        }

        private string? ComputePublicKeyString(X509Certificate2? certificate)
        {
            if (certificate == null)
            {
                return null;
            }
            // We Create the Pop public key
            var key = new X509SecurityKey(certificate);
            string base64EncodedJwk = Base64UrlEncoder.Encode(key.ComputeJwkThumbprint());
            var reqCnf = $@"{{""kid"":""{base64EncodedJwk}""}}";
            // 1.4. Base64 encode it again
            var keyId = Base64UrlEncoder.Encode(reqCnf);
            return keyId;
        }

        private static async Task CreateGraphClientAndAssertAsync(TokenAcquirerFactory tokenAcquirerFactory, IServiceCollection services)
        {
            services.AddInMemoryTokenCaches();
            services.AddMicrosoftGraph();
            var serviceProvider = tokenAcquirerFactory.Build();

            GraphServiceClient graphServiceClient = serviceProvider.GetRequiredService<GraphServiceClient>();
/*
            var users = await graphServiceClient.Users
                .GetAsync(o => o.Options
                                 .WithAppOnly()
                                 .WithAuthenticationScheme(s_optionName));
            Assert.True(users!=null && users.Value!=null && users.Value.Count >0);
*/


            // Alternatively to calling Microsoft Graph, you can get a token acquirer service
            // and get a token, and use it in an SDK.
            ITokenAcquirer tokenAcquirer = tokenAcquirerFactory.GetTokenAcquirer(s_optionName);
            var result = await tokenAcquirer.GetTokenForAppAsync("https://graph.microsoft.com/.default");
            Assert.NotNull(result.AccessToken);
        }
    }

    public class AcquireTokenManagedIdentity
    {
        [OnlyOnAzureDevopsFact]
        //[Fact]
        public async Task AcquireTokenWithManagedIdentity_UserAssignedAsync()
        {
            // Arrange
            const string scope = "https://vault.azure.net/.default";
            const string baseUrl = "https://vault.azure.net";
            const string clientId = "5bcd1685-b002-4fd1-8ebd-1ec3e1e4ca4d";
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();

            // Act: Get the authorization header provider and add the options to tell it to use Managed Identity
            IAuthorizationHeaderProvider? api = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            Assert.NotNull(api);
            string result = await api.CreateAuthorizationHeaderForAppAsync(scope, GetAuthHeaderOptions_ManagedId(baseUrl, clientId));

            // Assert: Make sure we got a token
            Assert.False(string.IsNullOrEmpty(result));

            result = await api.CreateAuthorizationHeaderAsync([scope], GetAuthHeaderOptions_ManagedId(baseUrl, clientId));
            Assert.False(string.IsNullOrEmpty(result));
        }

        private static AuthorizationHeaderProviderOptions GetAuthHeaderOptions_ManagedId(string baseUrl, string? userAssignedClientId = null)
        {
            ManagedIdentityOptions managedIdentityOptions = new()
            {
                UserAssignedClientId = userAssignedClientId
            };
            AcquireTokenOptions aquireTokenOptions = new()
            {
                ManagedIdentity = managedIdentityOptions
            };
            return new AuthorizationHeaderProviderOptions()
            {
                BaseUrl = baseUrl,
                AcquireTokenOptions = aquireTokenOptions
            };
        }
    }
#endif //FROM_GITHUB_ACTION
}
