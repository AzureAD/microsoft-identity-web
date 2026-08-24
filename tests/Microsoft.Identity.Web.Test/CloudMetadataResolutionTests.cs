// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Unit tests for <see cref="CloudMetadataResolution"/>: the ID Web translator that layers a per-call
    /// override over an upstream <see cref="ICloudMetadataProvider"/> (caller / upstream SDK) over MSAL's
    /// public baseline, keyed by authority host.
    /// </summary>
    public class CloudMetadataResolutionTests
    {
        private const string PublicHost = "login.microsoftonline.com";
        private const string UsGovAuthority = "https://login.microsoftonline.us/tenant";
        private const string UsGovHost = "login.microsoftonline.us";
        private const string NewCloudAuthority = "https://login.partner.example/tenant";
        private const string NewCloudHost = "login.partner.example";

        [Fact]
        public void ResolveAudience_NoAuthorityHost_ReturnsDocumentedDefault()
        {
            // A null/empty authority (for example the managed-identity leg, which has no AAD authority)
            // resolves to the documented public-cloud default rather than throwing.
            Assert.Equal(
                "api://AzureADTokenExchange",
                CloudMetadataResolution.ResolveTokenExchangeAudience(null, perCallOverride: null, upstreamProvider: null));
            Assert.Equal(
                "api://AzureADTokenExchange",
                CloudMetadataResolution.ResolveTokenExchangeAudience(string.Empty, perCallOverride: null, upstreamProvider: null));
        }

        [Fact]
        public void ResolveAudience_UnknownHost_NoProvider_Throws()
        {
            // W5 decision: a non-empty authority host that nothing recognizes fails loudly rather than
            // silently exchanging against the wrong (public-cloud) audience.
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                CloudMetadataResolution.ResolveTokenExchangeAudience(NewCloudAuthority, perCallOverride: null, upstreamProvider: null));

            Assert.Contains(NewCloudHost, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void ResolveAudience_KnownSovereignHost_NoProvider_ResolvesFromMsalBaseline()
        {
            string audience = CloudMetadataResolution.ResolveTokenExchangeAudience(
                UsGovAuthority, perCallOverride: null, upstreamProvider: null);

            Assert.Equal("api://AzureADTokenExchangeUSGov", audience);
        }

        [Fact]
        public void ResolveAudience_UpstreamProvider_WinsOverMsalBaseline()
        {
            // Upstream overrides a cloud MSAL already ships (US Gov).
            var provider = new InMemoryCloudMetadataProvider().AddOrUpdate(
                UsGovHost,
                new Dictionary<string, string>
                {
                    [CloudMetadataKeyNames.FederatedCredentialAudience] = "api://AzureADTokenExchangeCustomGov",
                });

            string audience = CloudMetadataResolution.ResolveTokenExchangeAudience(
                UsGovAuthority, perCallOverride: null, provider);

            Assert.Equal("api://AzureADTokenExchangeCustomGov", audience);
        }

        [Fact]
        public void ResolveAudience_UpstreamProvider_ResolvesNewCloudUnknownToMsal()
        {
            // A brand-new cloud MSAL does not ship becomes resolvable via the upstream provider.
            var provider = new InMemoryCloudMetadataProvider().AddOrUpdate(
                NewCloudHost,
                new Dictionary<string, string>
                {
                    [CloudMetadataKeyNames.FederatedCredentialAudience] = "api://AzureADTokenExchangeMyCloud",
                });

            string audience = CloudMetadataResolution.ResolveTokenExchangeAudience(
                NewCloudAuthority, perCallOverride: null, provider);

            Assert.Equal("api://AzureADTokenExchangeMyCloud", audience);
        }

        [Fact]
        public void ResolveAudience_PerCallOverride_WinsOverProviderAndBaseline()
        {
            var provider = new InMemoryCloudMetadataProvider().AddOrUpdate(
                UsGovHost,
                new Dictionary<string, string>
                {
                    [CloudMetadataKeyNames.FederatedCredentialAudience] = "api://AzureADTokenExchangeCustomGov",
                });

            string audience = CloudMetadataResolution.ResolveTokenExchangeAudience(
                UsGovAuthority, perCallOverride: "api://PerCallAudience", provider);

            Assert.Equal("api://PerCallAudience", audience);
        }

        [Fact]
        public void ResolveScope_AppendsDefaultSuffix_Idempotently()
        {
            string fromBare = CloudMetadataResolution.ResolveTokenExchangeScope(
                PublicHost, perCallOverride: "api://Aud", upstreamProvider: null);
            string fromSuffixed = CloudMetadataResolution.ResolveTokenExchangeScope(
                PublicHost, perCallOverride: "api://Aud/.default", upstreamProvider: null);

            Assert.Equal("api://Aud/.default", fromBare);
            Assert.Equal("api://Aud/.default", fromSuffixed);
        }

        [Fact]
        public void ResolveScope_KnownSovereignHost_ComputesScopeFromBaseline()
        {
            string scope = CloudMetadataResolution.ResolveTokenExchangeScope(
                UsGovAuthority, perCallOverride: null, upstreamProvider: null);

            Assert.Equal("api://AzureADTokenExchangeUSGov/.default", scope);
        }

        [Fact]
        public void AddCloudMetadata_FromConfiguration_RegistersResolvableProvider()
        {
            // A new cloud MSAL/ID Web do not ship, supplied entirely from an appsettings-style section.
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"CloudMetadata:{NewCloudHost}:{CloudMetadataKeyNames.FederatedCredentialAudience}"] = "api://AzureADTokenExchangeMyCloud",
                })
                .Build();
            var services = new ServiceCollection();

            // Act
            services.AddCloudMetadata(config.GetSection("CloudMetadata"));
            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Assert: the provider is registered and drives resolution end-to-end (audience + scope).
            var provider = serviceProvider.GetRequiredService<ICloudMetadataProvider>();
            Assert.Equal(
                "api://AzureADTokenExchangeMyCloud",
                provider.GetByAuthorityHost(NewCloudHost)![CloudMetadataKeyNames.FederatedCredentialAudience]);

            Assert.Equal(
                "api://AzureADTokenExchangeMyCloud",
                CloudMetadataResolution.ResolveTokenExchangeAudience(NewCloudAuthority, perCallOverride: null, provider));
            Assert.Equal(
                "api://AzureADTokenExchangeMyCloud/.default",
                CloudMetadataResolution.ResolveTokenExchangeScope(NewCloudAuthority, perCallOverride: null, provider));
        }
    }
}
