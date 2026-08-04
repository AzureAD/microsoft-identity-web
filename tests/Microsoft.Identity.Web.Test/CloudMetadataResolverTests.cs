// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using NSubstitute;
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
            var resolver = new CloudMetadataResolver(upstreamProvider: null);

            // Act
            string audience = resolver.ResolveTokenExchangeAudience(NewCloudAuthority);

            // Assert
            Assert.Equal("api://AzureADTokenExchange", audience);
        }

        [Fact]
        public void ResolveAudience_KnownSovereignHost_NoProvider_ResolvesFromMsalBaseline()
        {
            // Arrange
            var resolver = new CloudMetadataResolver(upstreamProvider: null);

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
            var resolver = new CloudMetadataResolver(provider);

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
            var resolver = new CloudMetadataResolver(provider);

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
            var resolver = new CloudMetadataResolver(provider);

            // Act
            string audience = resolver.ResolveTokenExchangeAudience(UsGovAuthority, perCallOverride: "api://PerCallAudience");

            // Assert
            Assert.Equal("api://PerCallAudience", audience);
        }

        [Fact]
        public void ResolveScope_AppendsDefaultSuffix_Idempotently()
        {
            // Arrange
            var resolver = new CloudMetadataResolver(upstreamProvider: null);

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
            var resolver = new CloudMetadataResolver(upstreamProvider: null);

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
            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Act
            CloudMetadataResolver resolver = CloudMetadataResolver.FromServiceProvider(serviceProvider);

            // Assert
            Assert.Equal("api://AzureADTokenExchangeMyCloud", resolver.ResolveTokenExchangeAudience(NewCloudAuthority));
            Assert.Equal("api://AzureADTokenExchangeUSGov", resolver.ResolveTokenExchangeAudience(UsGovAuthority));
        }

        [Fact]
        public void AddCloudMetadata_FromConfiguration_RegistersResolvableProvider()
        {
            // Arrange: a new cloud MSAL/ID Web do not ship, supplied entirely from an appsettings-style section.
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"CloudMetadata:{NewCloudHost}:{AbstractionsCloudKeys.TokenExchangeAudience}"] = "api://AzureADTokenExchangeMyCloud",
                })
                .Build();
            var services = new ServiceCollection();

            // Act
            services.AddCloudMetadata(config.GetSection("CloudMetadata"));
            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Assert: the provider is registered and the resolver honors it end-to-end (audience + scope).
            var provider = serviceProvider.GetRequiredService<ICloudMetadataProvider>();
            Assert.Equal(
                "api://AzureADTokenExchangeMyCloud",
                provider.GetByAuthorityHost(NewCloudHost)!.GetValueOrDefault(AbstractionsCloudKeys.TokenExchangeAudience));

            CloudMetadataResolver resolver = CloudMetadataResolver.FromServiceProvider(serviceProvider);
            Assert.Equal("api://AzureADTokenExchangeMyCloud", resolver.ResolveTokenExchangeAudience(NewCloudAuthority));
            Assert.Equal("api://AzureADTokenExchangeMyCloud/.default", resolver.ResolveTokenExchangeScope(NewCloudAuthority));
        }

        [Fact]
        public void ResolveAudience_UnknownNonPublicHost_WithLogger_WarnsOncePerHost()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CloudMetadataResolver>>();
            var resolver = new CloudMetadataResolver(upstreamProvider: null, logger);

            // Act: an unrecognized (non-public) host falls back to the documented public default...
            string audience = resolver.ResolveTokenExchangeAudience(NewCloudAuthority);
            // ...and a second lookup for the same host must not warn again (deduped).
            resolver.ResolveTokenExchangeAudience(NewCloudAuthority);

            // Assert
            Assert.Equal("api://AzureADTokenExchange", audience);
            Assert.Equal(1, CountWarnings(logger));
        }

        [Fact]
        public void ResolveAudience_PublicOrKnownHost_WithLogger_DoesNotWarn()
        {
            // Arrange
            var logger = Substitute.For<ILogger<CloudMetadataResolver>>();
            var resolver = new CloudMetadataResolver(upstreamProvider: null, logger);

            // Act: public cloud and a sovereign cloud MSAL ships both resolve without a fallback warning.
            resolver.ResolveTokenExchangeAudience("https://login.microsoftonline.com/tenant");
            resolver.ResolveTokenExchangeAudience(UsGovAuthority);

            // Assert
            Assert.Equal(0, CountWarnings(logger));
        }

        private static int CountWarnings(ILogger logger) =>
            logger.ReceivedCalls().Count(call =>
            {
                object?[] args = call.GetArguments();
                return args.Length > 0 && args[0] is LogLevel level && level == LogLevel.Warning;
            });
    }
}
