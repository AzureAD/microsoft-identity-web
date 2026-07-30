// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Web;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Unit tests for <see cref="CloudMetadataResolver"/>: the ID Web translator that layers a per-call
    /// override over an upstream <see cref="ICloudMetadataProvider"/> (caller / MISE) over MSAL's public
    /// baseline, keyed by authority host.
    /// </summary>
    public class CloudMetadataResolverTests
    {
        private const string PublicHost = "login.microsoftonline.com";
        private const string UsGovAuthority = "https://login.microsoftonline.us/tenant";
        private const string UsGovHost = "login.microsoftonline.us";
        private const string NewCloudAuthority = "https://login.partner.example/tenant";
        private const string NewCloudHost = "login.partner.example";

        [Fact]
        public void ResolveAudience_UnknownHost_NoProvider_ReturnsDocumentedDefault()
        {
            // Arrange
            var resolver = new CloudMetadataResolver(upstreamProvider: null, msalBaseline: null);

            // Act
            string audience = resolver.ResolveTokenExchangeAudience(NewCloudAuthority);

            // Assert
            Assert.Equal("api://AzureADTokenExchange", audience);
        }

        [Fact]
        public void ResolveAudience_KnownSovereignHost_NoProvider_ResolvesFromMsalBaseline()
        {
            // Arrange
            var resolver = new CloudMetadataResolver(upstreamProvider: null, msalBaseline: null);

            // Act
            string audience = resolver.ResolveTokenExchangeAudience(UsGovAuthority);

            // Assert
            Assert.Equal("api://AzureADTokenExchangeUSGov", audience);
        }

        [Fact]
        public void ResolveAudience_UpstreamProvider_WinsOverMsalBaseline()
        {
            // Arrange: upstream overrides a cloud MSAL already ships (US Gov).
            var provider = new InMemoryCloudMetadataProvider().AddOrUpdate(
                UsGovHost,
                new Dictionary<string, string>
                {
                    [AbstractionsCloudKeys.TokenExchangeAudience] = "api://AzureADTokenExchangeCustomGov",
                });
            var resolver = new CloudMetadataResolver(provider, msalBaseline: null);

            // Act
            string audience = resolver.ResolveTokenExchangeAudience(UsGovAuthority);

            // Assert
            Assert.Equal("api://AzureADTokenExchangeCustomGov", audience);
        }

        [Fact]
        public void ResolveAudience_UpstreamProvider_ResolvesNewCloudUnknownToMsal()
        {
            // Arrange: a brand-new cloud MSAL does not ship becomes resolvable via the upstream provider.
            var provider = new InMemoryCloudMetadataProvider().AddOrUpdate(
                NewCloudHost,
                new Dictionary<string, string>
                {
                    [AbstractionsCloudKeys.TokenExchangeAudience] = "api://AzureADTokenExchangeMyCloud",
                });
            var resolver = new CloudMetadataResolver(provider, msalBaseline: null);

            // Act
            string audience = resolver.ResolveTokenExchangeAudience(NewCloudAuthority);

            // Assert
            Assert.Equal("api://AzureADTokenExchangeMyCloud", audience);
        }

        [Fact]
        public void ResolveAudience_PerCallOverride_WinsOverProviderAndBaseline()
        {
            // Arrange
            var provider = new InMemoryCloudMetadataProvider().AddOrUpdate(
                UsGovHost,
                new Dictionary<string, string>
                {
                    [AbstractionsCloudKeys.TokenExchangeAudience] = "api://AzureADTokenExchangeCustomGov",
                });
            var resolver = new CloudMetadataResolver(provider, msalBaseline: null);

            // Act
            string audience = resolver.ResolveTokenExchangeAudience(UsGovAuthority, perCallOverride: "api://PerCallAudience");

            // Assert
            Assert.Equal("api://PerCallAudience", audience);
        }

        [Fact]
        public void ResolveScope_AppendsDefaultSuffix_Idempotently()
        {
            // Arrange
            var resolver = new CloudMetadataResolver(upstreamProvider: null, msalBaseline: null);

            // Act
            string fromBare = resolver.ResolveTokenExchangeScope(PublicHost, perCallOverride: "api://Aud");
            string fromSuffixed = resolver.ResolveTokenExchangeScope(PublicHost, perCallOverride: "api://Aud/.default");

            // Assert
            Assert.Equal("api://Aud/.default", fromBare);
            Assert.Equal("api://Aud/.default", fromSuffixed);
        }

        [Fact]
        public void ResolveScope_KnownSovereignHost_ComputesScopeFromBaseline()
        {
            // Arrange
            var resolver = new CloudMetadataResolver(upstreamProvider: null, msalBaseline: null);

            // Act
            string scope = resolver.ResolveTokenExchangeScope(UsGovAuthority);

            // Assert
            Assert.Equal("api://AzureADTokenExchangeUSGov/.default", scope);
        }

        [Fact]
        public void FromServiceProvider_HonorsRegisteredUpstreamProvider()
        {
            // Arrange
            var provider = new InMemoryCloudMetadataProvider().AddOrUpdate(
                NewCloudHost,
                new Dictionary<string, string>
                {
                    [AbstractionsCloudKeys.TokenExchangeAudience] = "api://AzureADTokenExchangeMyCloud",
                });
            var services = new ServiceCollection();
            services.AddSingleton<ICloudMetadataProvider>(provider);
            services.AddSingleton<ICloudConfiguration>(KnownCloudConfiguration.Default);
            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Act
            CloudMetadataResolver resolver = CloudMetadataResolver.FromServiceProvider(serviceProvider);

            // Assert
            Assert.Equal("api://AzureADTokenExchangeMyCloud", resolver.ResolveTokenExchangeAudience(NewCloudAuthority));
            Assert.Equal("api://AzureADTokenExchangeUSGov", resolver.ResolveTokenExchangeAudience(UsGovAuthority));
        }
    }
}
