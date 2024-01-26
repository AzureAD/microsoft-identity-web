// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Lab.Api;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common.TestHelpers;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Microsoft.Identity.Web.Test.Integration
{
    public class DownstreamApiForAppCaeTests
    {
        private readonly TimeSpan _delayTimeout = TimeSpan.FromMinutes(5);
        private ServiceProvider _provider;
        private IDownstreamApi _downstreamApi;
        private readonly string _ccaSecret;
        private MsalTestTokenCacheProvider _msalTestTokenCacheProvider;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public DownstreamApiForAppCaeTests()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            KeyVaultSecretsProvider keyVaultSecretsProvider = new(TestConstants.BuildAutomationKeyVaultName);
            _ccaSecret = keyVaultSecretsProvider.GetSecretByName(TestConstants.AzureADIdentityDivisionTestAgentSecret).Value;

            // Need the secret before building the services
            if (!string.IsNullOrEmpty(_ccaSecret))
            {
                BuildRequiredServices();
            }
            else
            {
                throw new ArgumentNullException(message: "No secret returned from Key Vault. ", null);
            }
        }

        /// <summary>
        /// Enable the app's service principal.
        /// Call DownstreamApi.GetForAppAsync to Graph's /users endpoint. Internally:
        /// - Gets a token from AAD.
        /// - Calls Graph with the token; should receive a successful response.
        /// Disable the CAE app's service principal and wait until the changes propagate to Graph.
        /// Call DownstreamApi.GetForAppAsync again. Internally:
        /// - Gets a token from cache.
        /// - Calls Graph again; should receive a 401 with claims.
        /// - Tries to acquire a new token with claims; should receive an exception that the SP is disabled.
        /// Enable the app's service principal.
        /// </summary>
        [Fact]
        public async Task ClientCredentials_WithDisabledServicePrincipal_ThrowsException()
        {
            Assert.Equal(0, _msalTestTokenCacheProvider.Count);

            await LabUserHelper.EnableAppServicePrincipal(LabApiConstants.LabCaeConfidentialClientId);

            var result = await _downstreamApi.GetForAppAsync<EmptyClass>("GraphApp",
                options => options.RelativePath = "users");

            await LabUserHelper.DisableAppServicePrincipal(LabApiConstants.LabCaeConfidentialClientId);
            await Task.Delay(_delayTimeout).ConfigureAwait(false);

            var ex = await Assert.ThrowsAsync<MsalServiceException>(() =>
                _downstreamApi.GetForAppAsync<EmptyClass>("GraphApp",
                    options => options.RelativePath = "users"));

            Assert.Equal(MsalError.UnauthorizedClient, ex.ErrorCode); // AADSTS7000112 Service principal is disabled

            await LabUserHelper.EnableAppServicePrincipal(LabApiConstants.LabCaeConfidentialClientId);
        }

        private void BuildRequiredServices()
        {
            var microsoftIdentityOptionsMonitor = new TestOptionsMonitor<MicrosoftIdentityOptions>(new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AadInstance + "/" + TestConstants.ConfidentialClientLabTenant,
                ClientId = TestConstants.LabCaeConfidentialClientId,
                CallbackPath = string.Empty,
            });
            var applicationOptionsMonitor = new TestOptionsMonitor<ConfidentialClientApplicationOptions>(new ConfidentialClientApplicationOptions
            {
                Instance = TestConstants.AadInstance,
                TenantId = TestConstants.ConfidentialClientLabTenant,
                ClientId = TestConstants.LabCaeConfidentialClientId,
                ClientSecret = _ccaSecret,
            });

            var services = new ServiceCollection();

            services.AddTokenAcquisition();
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme);
            services.AddTransient(provider => microsoftIdentityOptionsMonitor);
            services.AddTransient(provider => applicationOptionsMonitor);
            services.Configure<MergedOptions>(OpenIdConnectDefaults.AuthenticationScheme, options => { });
            services.AddLogging();
            services.AddInMemoryTokenCaches();
            services.AddHttpClient();
            services.AddDownstreamApi("GraphApp", options => {
                options.Scopes = new[] { "https://graph.microsoft.com/.default" };
                options.BaseUrl = "https://graph.microsoft.com/v1.0";
            });
            _provider = services.BuildServiceProvider();

            var credentialsLoader = new DefaultCredentialsLoader();
            MergedOptions mergedOptions = _provider.GetRequiredService<IMergedOptionsStore>().Get(OpenIdConnectDefaults.AuthenticationScheme);

            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(microsoftIdentityOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(applicationOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme), mergedOptions);

            _msalTestTokenCacheProvider = new MsalTestTokenCacheProvider(
                 _provider.GetService<IMemoryCache>()!,
                 _provider.GetService<IOptions<MsalMemoryTokenCacheOptions>>()!);

            var tokenAcquisitionAspnetCoreHost = new TokenAcquisitionAspnetCoreHost(
                MockHttpContextAccessor.CreateMockHttpContextAccessor(),
                _provider.GetService<IMergedOptionsStore>()!,
                _provider);

            tokenAcquisitionAspnetCoreHost.GetOptions(OpenIdConnectDefaults.AuthenticationScheme, out _);
        }

        // Placeholder for a generic type
        private class EmptyClass { }
    }
}
