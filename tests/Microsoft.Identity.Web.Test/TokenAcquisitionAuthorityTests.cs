// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;
using TC = Microsoft.Identity.Web.Test.Common.TestConstants;

namespace Microsoft.Identity.Web.Test
{
    public class TokenAcquisitionAuthorityTests
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private TokenAcquisition _tokenAcquisition;
        private TokenAcquisitionAspnetCoreHost _tokenAcquisitionAspnetCoreHost;
        private ServiceProvider _provider;
        private IOptionsMonitor<ConfidentialClientApplicationOptions> _applicationOptionsMonitor;
        private IOptionsMonitor<MicrosoftIdentityOptions> _microsoftIdentityOptionsMonitor;
        private ICredentialsLoader _credentialsLoader;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        private void InitializeTokenAcquisitionObjects()
        {
            _credentialsLoader = new DefaultCredentialsLoader();
            _tokenAcquisitionAspnetCoreHost = new TokenAcquisitionAspnetCoreHost(
                MockHttpContextAccessor.CreateMockHttpContextAccessor(),
                _provider.GetService<IMergedOptionsStore>()!,
                _provider);
            _tokenAcquisition = new TokenAcquisitionAspNetCore(
                new MsalTestTokenCacheProvider(
                _provider.GetService<IMemoryCache>()!,
                _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>()!),
                _provider.GetService<IHttpClientFactory>()!,
                _provider.GetService<ILogger<TokenAcquisition>>()!,
                _tokenAcquisitionAspnetCoreHost,
                _provider,
                _credentialsLoader);
        }

        private void BuildTheRequiredServices()
        {
            var services = new ServiceCollection();
            services.AddTransient(
                provider => _microsoftIdentityOptionsMonitor);
            services.AddTransient(
                provider => _applicationOptionsMonitor);
            services.Configure<MergedOptions>(options => { });
            services.AddTokenAcquisition();
            services.AddLogging();
            services.AddAuthentication();
            _provider = services.BuildServiceProvider();
        }

        [Theory]
        [InlineData(JwtBearerDefaults.AuthenticationScheme)]
        [InlineData(OpenIdConnectDefaults.AuthenticationScheme)]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
        [InlineData(null)]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
        public void VerifyCorrectSchemeTests(string scheme)
        {
            BuildTheRequiredServices();
            InitializeTokenAcquisitionObjects();

            if (!string.IsNullOrEmpty(scheme))
            {
                Assert.Equal(scheme, _tokenAcquisition.GetEffectiveAuthenticationScheme(scheme));
            }
            else
            {
                Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, _tokenAcquisition.GetEffectiveAuthenticationScheme(scheme));
            }
        }

        [Theory]
        [InlineData(TC.B2CInstance)]
        [InlineData(TC.B2CLoginMicrosoft)]
        [InlineData(TC.B2CInstance, true)]
        [InlineData(TC.B2CLoginMicrosoft, true)]
        public async Task VerifyCorrectAuthorityUsedInTokenAcquisition_B2CAuthorityTestsAsync(
            string authorityInstance,
            bool withTfp = false)
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                SignUpSignInPolicyId = TC.B2CSignUpSignInUserFlow,
                Domain = TC.B2CTenant,
            });

            if (withTfp)
            {
                _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
                {
                    Instance = authorityInstance + "/tfp/",
                    ClientId = TC.ConfidentialClientId,
                    ClientSecret = TC.ClientSecret,
                });
            }
            else
            {
                _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
                {
                    Instance = authorityInstance,
                    ClientId = TC.ConfidentialClientId,
                    ClientSecret = TC.ClientSecret,
                });
            }

            BuildTheRequiredServices();
            MergedOptions mergedOptions = _provider.GetRequiredService<IMergedOptionsStore>().Get(OpenIdConnectDefaults.AuthenticationScheme);
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(_applicationOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            InitializeTokenAcquisitionObjects();

            IConfidentialClientApplication app = await _tokenAcquisition.GetOrBuildConfidentialClientApplicationAsync(mergedOptions);

            string expectedAuthority = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/tfp/{1}/{2}/",
                authorityInstance,
                TC.B2CTenant,
                TC.B2CSignUpSignInUserFlow);

            Assert.Equal(expectedAuthority, app.Authority);
        }

        [Theory]
        [InlineData("https://localhost:1234")]
        [InlineData("")]
        public async Task VerifyCorrectRedirectUriAsync(
            string redirectUri)
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                Authority = TC.AuthorityCommonTenant,
                ClientId = TC.ConfidentialClientId,
                CallbackPath = string.Empty,
            });

            _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
            {
                Instance = TC.AadInstance,
                RedirectUri = redirectUri,
                ClientSecret = TC.ClientSecret,
            });

            BuildTheRequiredServices();
            MergedOptions mergedOptions = _provider.GetRequiredService<IMergedOptionsStore>().Get(OpenIdConnectDefaults.AuthenticationScheme);
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(_applicationOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            InitializeTokenAcquisitionObjects();

            IConfidentialClientApplication app = await _tokenAcquisition.GetOrBuildConfidentialClientApplicationAsync(mergedOptions);

            if (!string.IsNullOrEmpty(redirectUri))
            {
                Assert.Equal(redirectUri, app.AppConfig.RedirectUri);
            }
            else
            {
                Assert.Equal("https://IdentityDotNetSDKAutomation/", app.AppConfig.RedirectUri);
            }
        }

        [Fact]
        public async Task VerifyDifferentRegionsDifferentAppAsync()
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                Authority = TC.AuthorityCommonTenant,
                ClientId = TC.ConfidentialClientId,
                CallbackPath = string.Empty,
            });

            _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
            {
                Instance = TC.AadInstance,
                RedirectUri = "http://localhost:1729/",
                ClientSecret = TC.ClientSecret,
            });

            BuildTheRequiredServices();
            MergedOptions mergedOptions = _provider.GetRequiredService<IMergedOptionsStore>().Get(OpenIdConnectDefaults.AuthenticationScheme);
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(_applicationOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            InitializeTokenAcquisitionObjects();

            mergedOptions.AzureRegion = "UKEast";

            IConfidentialClientApplication appEast = await _tokenAcquisition.GetOrBuildConfidentialClientApplicationAsync(mergedOptions);

            mergedOptions.AzureRegion = "UKWest";

            IConfidentialClientApplication appWest = await _tokenAcquisition.GetOrBuildConfidentialClientApplicationAsync(mergedOptions);

            Assert.NotSame(appEast, appWest);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyCorrectBooleans(
           bool sendx5c)
        {
            _microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                Authority = TC.AuthorityCommonTenant,
                ClientId = TC.ConfidentialClientId,
                SendX5C = sendx5c,
            });

            _applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
            {
                Instance = TC.AadInstance,
                ClientSecret = TC.ClientSecret,
            });

            BuildTheRequiredServices();
            MergedOptions mergedOptions = _provider.GetRequiredService<IMergedOptionsStore>().Get(OpenIdConnectDefaults.AuthenticationScheme);
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(_microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(_applicationOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            InitializeTokenAcquisitionObjects();

            if (sendx5c)
            {
                Assert.True(mergedOptions.SendX5C);
            }
            else
            {
                Assert.False(mergedOptions.SendX5C);
            }
        }

        [Fact]
        public void TestParseAuthorityIfNecessary()
        {
            // Arrange
            MergedOptions mergedOptions = new()
            {
                Authority = TC.AuthorityWithTenantSpecified,
                TenantId = TC.TenantIdAsGuid,
                Instance = TC.AadInstance
            };

            // Act
            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            // Assert
            Assert.Equal(TC.AuthorityWithTenantSpecified, mergedOptions.Authority);
            Assert.Equal(TC.AadInstance, mergedOptions.Instance);
            Assert.Equal(TC.TenantIdAsGuid, mergedOptions.TenantId);
        }

        [Fact]
        public void TestParseAuthorityIfNecessary_CIAM()
        {
            MergedOptions mergedOptions = new()
            {
                Authority = TC.CIAMAuthorityV2,
                PreserveAuthority = true
            };

            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            Assert.Equal(TC.CIAMAuthorityV2, mergedOptions.Authority);
            Assert.Equal(TC.CIAMAuthorityV2, mergedOptions.Instance);
            Assert.Null(mergedOptions.TenantId);
        }

        [Fact]
        public void TestParseAuthority_PreserveAuthorityFalse_CIAM()
        {
            MergedOptions mergedOptions = new()
            {
                Authority = TC.CIAMAuthority,
                PreserveAuthority = false
            };

            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            Assert.Equal(TC.CIAMAuthority, mergedOptions.Authority);
            Assert.Equal(TC.CIAMInstance, mergedOptions.Instance);
            Assert.Equal(TC.CIAMTenant, mergedOptions.TenantId);
        }

        [Fact]
        public void TestParseAuthorityIfNecessary_V2Authority()
        {
            MergedOptions mergedOptions = new()
            {
                Authority = TC.AuthorityWithTenantSpecifiedWithV2,
                PreserveAuthority = false
            };

            MergedOptions.ParseAuthorityIfNecessary(mergedOptions);

            Assert.Equal(TC.AuthorityWithTenantSpecifiedWithV2, mergedOptions.Authority);
            Assert.Equal(TC.AadInstance, mergedOptions.Instance);
            Assert.Equal(TC.TenantIdAsGuid, mergedOptions.TenantId);
        }

        [Fact]
        public void MergeExtraQueryParametersTest()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                ExtraQueryParameters = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            }
            };
            var tokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                ExtraQueryParameters = new Dictionary<string, string>
            {
                { "key1", "newvalue1" },
                { "key3", "value3" }
            }
            };

            // Act
            var mergedDict = TokenAcquisition.MergeExtraQueryParameters(mergedOptions, tokenAcquisitionOptions);


            // Assert
            Assert.Equal(3, mergedDict!.Count);
            Assert.Equal("newvalue1", mergedDict["key1"]);
            Assert.Equal("value2", mergedDict["key2"]);
            Assert.Equal("value3", mergedDict["key3"]);
        }

        [Fact]
        public void MergeExtraQueryParameters_TokenAcquisitionOptionsNull_Test()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                ExtraQueryParameters = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            }
            };
            var tokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                ExtraQueryParameters = null,
            };

            // Act
            var mergedDict = TokenAcquisition.MergeExtraQueryParameters(mergedOptions, tokenAcquisitionOptions);

            // Assert
            Assert.Equal("value1", mergedDict!["key1"]);
            Assert.Equal("value2", mergedDict["key2"]);
        }

        [Fact]
        public void MergeExtraQueryParameters_MergedOptionsNull_Test()
        {
            // Arrange
            var mergedOptions = new MergedOptions
            {
                ExtraQueryParameters = null,
            };
            var tokenAcquisitionOptions = new TokenAcquisitionOptions
            {
                ExtraQueryParameters = null,
            };

            // Act
            var mergedDict = TokenAcquisition.MergeExtraQueryParameters(mergedOptions, tokenAcquisitionOptions);

            // Assert
            Assert.Null(mergedDict);
        }

        [Theory]
        [InlineData("https://localhost:1234")]
        [InlineData("")]
        [InlineData(null)]
        public void ManagedIdCacheKey_Test(string? clientId)
        {
            // Arrange
            string defaultKey = "SYSTEM";
            ManagedIdentityOptions managedIdentityOptions = new()
            {
                UserAssignedClientId = clientId
            };

            // Act
            string key = TokenAcquisition.GetCacheKeyForManagedId(managedIdentityOptions);

            // Assert
            if (string.IsNullOrEmpty(clientId))
            {
                Assert.Equal(defaultKey, key);
            }
            else
            {
                Assert.Equal(clientId, key);
            }
        }

        [Theory]
        [InlineData("https://localhost:1234")]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetOrBuildManagedIdentity_TestAsync(string? clientId)
        {
            // Arrange
            ManagedIdentityOptions managedIdentityOptions = new()
            {
                UserAssignedClientId = clientId
            };
            MergedOptions mergedOptions = new();
            BuildTheRequiredServices();
            InitializeTokenAcquisitionObjects();

            // Act
            var app1 = 
                await _tokenAcquisition.GetOrBuildManagedIdentityApplicationAsync(mergedOptions, managedIdentityOptions);
            var app2 = 
                await _tokenAcquisition.GetOrBuildManagedIdentityApplicationAsync(mergedOptions, managedIdentityOptions);

            // Assert
            Assert.Same(app1, app2);
        }

        [Theory]
        [InlineData("https://localhost:1234")]
        [InlineData(null)]
        public async Task GetOrBuildManagedIdentity_TestConcurrencyAsync(string? clientId)
        {
            // Arrange
            ThreadPool.GetMaxThreads(out int maxThreads, out int _);
            ConcurrentBag<IManagedIdentityApplication> appsBag = [];
            CountdownEvent taskStartGate = new(maxThreads);
            CountdownEvent threadsDone = new(maxThreads);
            ManagedIdentityOptions managedIdentityOptions = new()
            {
                UserAssignedClientId = clientId
            };
            MergedOptions mergedOptions = new();
            BuildTheRequiredServices();
            InitializeTokenAcquisitionObjects();

            // Act
            for (int i = 0; i < maxThreads; i++)
            {
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
                Thread thread = new(async () =>
                {
                    try
                    {
                        // Signal that the thread is ready to start and wait for the other threads to be ready.
                        taskStartGate.Signal();
                        taskStartGate.Wait();

                        // Add the application to the bag
                        appsBag.Add(await _tokenAcquisition.GetOrBuildManagedIdentityApplicationAsync(mergedOptions, managedIdentityOptions));
                    }
                    finally
                    {
                        // No matter what happens, signal that the thread is done so the test doesn't get stuck.
                        threadsDone.Signal();
                    }
                });
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
                thread.Start();
            }
            threadsDone.Wait();
            var testApp = await _tokenAcquisition.GetOrBuildManagedIdentityApplicationAsync(mergedOptions, managedIdentityOptions);

            // Assert
            Assert.True(appsBag.Count == maxThreads, "Not all threads put objects in the concurrent bag");
            foreach (IManagedIdentityApplication app in appsBag)
            {
                Assert.Same(testApp, app);
            }
        }
    }
}
