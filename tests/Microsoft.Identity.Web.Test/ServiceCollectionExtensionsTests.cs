// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddTokenAcquisition_Sdk_AddsWithCorrectLifetime()
        {
            var services = new ServiceCollection();

            services.AddTokenAcquisition();
            ServiceDescriptor[] orderedServices = services.OrderBy(s => s.ServiceType.FullName).ToArray();

            Assert.Collection(
                orderedServices,
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<MicrosoftIdentityApplicationOptions>), actual.ServiceType);
                    Assert.Equal(typeof(MicrosoftIdentityApplicationOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                }, actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<ConfidentialClientApplicationOptions>), actual.ServiceType);
                    Assert.Equal(typeof(ConfidentialClientApplicationOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<MicrosoftIdentityOptions>), actual.ServiceType);
                    Assert.Equal(typeof(MicrosoftIdentityOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(Abstractions.IAuthenticationSchemeInformationProvider), actual.ServiceType);
                    Assert.Null(actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.NotNull(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(IAuthorizationHeaderProvider), actual.ServiceType);
                    Assert.Equal(typeof(DefaultAuthorizationHeaderProvider), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(typeof(ICredentialsLoader), actual.ServiceType);
                    Assert.Equal(typeof(DefaultCertificateLoader), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquirerFactory), actual.ServiceType);
                    Assert.Equal(typeof(DefaultTokenAcquirerFactoryImplementation), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IMergedOptionsStore), actual.ServiceType);
                    Assert.Equal(typeof(MergedOptionsStore), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
               {
                   Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                   Assert.Equal(typeof(ITokenAcquisition), actual.ServiceType);
                   Assert.Equal(typeof(TokenAcquisitionAspNetCore), actual.ImplementationType);
                   Assert.Null(actual.ImplementationInstance);
                   Assert.Null(actual.ImplementationFactory);
               },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquisitionHost), actual.ServiceType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquisitionInternal), actual.ServiceType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.NotNull(actual.ImplementationFactory);
                });
        }

#if NET8_0_OR_GREATER
        [Fact]
        public void AddTokenAcquisition_Sdk_SupportsKeyedServices()
        {
            var services = new ServiceCollection();

            // Add a keyed service.
            services.AddKeyedSingleton<ServiceCollectionExtensionsTests>("test", this);

            // This should not throw.
            services.AddTokenAcquisition();

            // Verify the number of services added by AddTokenAcquisition (ignoring the service we added here).
            Assert.Equal(11, services.Count(t => t.ServiceType != typeof(ServiceCollectionExtensionsTests)));
        }
#endif

        [Fact]
        public void AddTokenAcquisition_AbleToOverrideICredentialsLoader()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ICredentialsLoader, MockCredentialsLoader>();

            services.AddTokenAcquisition();

            ServiceDescriptor[] orderedServices = services.OrderBy(s => s.ServiceType.FullName).ToArray();

            Assert.Single(orderedServices, s => s.ServiceType == typeof(ICredentialsLoader));
        }

        [Fact]
        public void AddHttpContextAccessor_ThrowsWithoutServices()
        {
            Assert.Throws<ArgumentNullException>("services", () => ServiceCollectionExtensions.AddTokenAcquisition(null!));
        }

        [Fact]
        public void AddTokenAcquisitionCalledTwice_RegistersTokenAcquisitionOnlyAsSingleton_WhenFlagIsTrue()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            // Token acquisition services were originally registered as scoped
            services.AddTokenAcquisition();

            // Token acquisition services should now be registered as singleton
            services.AddTokenAcquisition(isTokenAcquisitionSingleton: true);

            // Assert
            var orderedServices = services.OrderBy(s => s.ServiceType.FullName).ToList();

            // Check that the first service is registered as singleton
            Assert.Collection(
                orderedServices,
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<MicrosoftIdentityApplicationOptions>), actual.ServiceType);
                    Assert.Equal(typeof(MicrosoftIdentityApplicationOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                }, actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<ConfidentialClientApplicationOptions>), actual.ServiceType);
                    Assert.Equal(typeof(ConfidentialClientApplicationOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<MicrosoftIdentityOptions>), actual.ServiceType);
                    Assert.Equal(typeof(MicrosoftIdentityOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(Abstractions.IAuthenticationSchemeInformationProvider), actual.ServiceType);
                    Assert.Null(actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.NotNull(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IAuthorizationHeaderProvider), actual.ServiceType);
                    Assert.Equal(typeof(DefaultAuthorizationHeaderProvider), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(typeof(ICredentialsLoader), actual.ServiceType);
                    Assert.Equal(typeof(DefaultCertificateLoader), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquirerFactory), actual.ServiceType);
                    Assert.Equal(typeof(DefaultTokenAcquirerFactoryImplementation), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IMergedOptionsStore), actual.ServiceType);
                    Assert.Equal(typeof(MergedOptionsStore), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquisition), actual.ServiceType);
                    Assert.Equal(typeof(TokenAcquisitionAspNetCore), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquisitionHost), actual.ServiceType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquisitionInternal), actual.ServiceType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.NotNull(actual.ImplementationFactory);
                });
        }

        [Fact]
        public void EnableTokenBinding_WithValidServiceCollection_RegistersIMsalHttpClientFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();

            // Act
            var result = services.EnableTokenBinding();

            // Assert
            Assert.Same(services, result); // Should return same instance for method chaining

            ServiceDescriptor? msalHttpClientFactoryService = services.FirstOrDefault(s => s.ServiceType == typeof(IMsalHttpClientFactory));
            Assert.NotNull(msalHttpClientFactoryService);
            Assert.Equal(ServiceLifetime.Singleton, msalHttpClientFactoryService.Lifetime);
            Assert.Null(msalHttpClientFactoryService.ImplementationType);
            Assert.NotNull(msalHttpClientFactoryService.ImplementationFactory);
        }

        [Fact]
        public void EnableTokenBinding_WithNullServiceCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IServiceCollection? services = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services!.EnableTokenBinding());
        }

        [Fact]
        public void EnableTokenBinding_CanResolveIMsalHttpClientFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.EnableTokenBinding();

            // Act
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var msalHttpClientFactory = serviceProvider.GetService<IMsalHttpClientFactory>();

            // Assert
            Assert.NotNull(msalHttpClientFactory);
            Assert.IsType<MsalMtlsHttpClientFactory>(msalHttpClientFactory);
        }

        [Fact]
        public void EnableTokenBinding_WithExistingIMsalHttpClientFactory_ReplacesWithMtlsFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            var mockFactory = new MockMsalHttpClientFactory();
            services.AddSingleton<IMsalHttpClientFactory>(mockFactory);

            // Act
            services.EnableTokenBinding();

            // Assert
            ServiceDescriptor[] msalHttpClientFactoryServices = services.Where(s => s.ServiceType == typeof(IMsalHttpClientFactory)).ToArray();
            Assert.Single(msalHttpClientFactoryServices); // Should still be only one registration

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedFactory = serviceProvider.GetRequiredService<IMsalHttpClientFactory>();

            // Should be the new MsalMtlsHttpClientFactory, not the original mock
            Assert.NotSame(mockFactory, resolvedFactory);
            Assert.IsType<MsalMtlsHttpClientFactory>(resolvedFactory);
        }

        [Fact]
        public void EnableTokenBinding_MultipleCalls_KeepsSingleRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();

            // Act
            services.EnableTokenBinding();
            services.EnableTokenBinding(); // Call again

            // Assert
            ServiceDescriptor[] msalHttpClientFactoryServices = services.Where(s => s.ServiceType == typeof(IMsalHttpClientFactory)).ToArray();
            Assert.Single(msalHttpClientFactoryServices); // Should still be only one registration

            // Verify that the factory can still be resolved and works correctly
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedFactory = serviceProvider.GetRequiredService<IMsalHttpClientFactory>();
            Assert.IsType<MsalMtlsHttpClientFactory>(resolvedFactory);
        }

        [Fact]
        public void EnableTokenBinding_WithoutIHttpClientFactory_ThrowsWhenResolvingFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.EnableTokenBinding(); // Add token binding without IHttpClientFactory

            // Act & Assert
            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // The factory should be registered, but resolving it should throw because IHttpClientFactory is missing
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMsalHttpClientFactory>());
        }

        [Fact]
        public void EnableTokenBinding_CreatedFactoryCanCreateHttpClient()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.EnableTokenBinding();

            // Act
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var msalHttpClientFactory = serviceProvider.GetRequiredService<IMsalHttpClientFactory>();

            // Assert
            using var httpClient = msalHttpClientFactory.GetHttpClient();
            Assert.NotNull(httpClient);
        }

        [Fact]
        public void EnableTokenBinding_RemovesExistingFactoryRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();

            // Add an initial mock factory
            var originalMockFactory = new MockMsalHttpClientFactory();
            services.AddSingleton<IMsalHttpClientFactory>(originalMockFactory);

            // Verify initial state
            Assert.Single(services, s => s.ServiceType == typeof(IMsalHttpClientFactory));

            // Act
            services.EnableTokenBinding();

            // Assert
            ServiceDescriptor[] msalHttpClientFactoryServices = services.Where(s => s.ServiceType == typeof(IMsalHttpClientFactory)).ToArray();
            Assert.Single(msalHttpClientFactoryServices); // Should still be only one registration

            // Verify the registration is not the original mock but a factory that creates MsalMtlsHttpClientFactory
            ServiceDescriptor factoryDescriptor = msalHttpClientFactoryServices[0];
            Assert.NotNull(factoryDescriptor.ImplementationFactory);
            Assert.Null(factoryDescriptor.ImplementationType);
            Assert.Null(factoryDescriptor.ImplementationInstance);

            // Verify the resolved factory is MsalMtlsHttpClientFactory, not the original mock
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedFactory = serviceProvider.GetRequiredService<IMsalHttpClientFactory>();
            Assert.IsType<MsalMtlsHttpClientFactory>(resolvedFactory);
            Assert.NotSame(originalMockFactory, resolvedFactory);
        }

        [Fact]
        public void EnableTokenBinding_WithNoExistingAuthorizationHeaderProvider_RegistersDefaultBoundProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.AddSingleton<ITokenAcquisition, MockTokenAcquisition>();

            // Act
            services.EnableTokenBinding();

            // Assert
            ServiceDescriptor? authHeaderProviderService = services.FirstOrDefault(s => s.ServiceType == typeof(IAuthorizationHeaderProvider));
            Assert.NotNull(authHeaderProviderService);
            Assert.Equal(ServiceLifetime.Scoped, authHeaderProviderService.Lifetime); // Default lifetime when no existing provider
            Assert.NotNull(authHeaderProviderService.ImplementationFactory);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            Assert.IsType<DefaultAuthorizationHeaderBoundProvider>(resolvedProvider);
        }

        [Fact]
        public void EnableTokenBinding_WithExistingScopedAuthorizationHeaderProvider_ReplacesWithBoundProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.AddSingleton<ITokenAcquisition, MockTokenAcquisition>();
            services.AddScoped<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();

            // Act
            services.EnableTokenBinding();

            // Assert
            ServiceDescriptor[] authHeaderProviderServices = services.Where(s => s.ServiceType == typeof(IAuthorizationHeaderProvider)).ToArray();
            Assert.Single(authHeaderProviderServices); // Should still be only one registration

            ServiceDescriptor authHeaderProviderService = authHeaderProviderServices[0];
            Assert.Equal(ServiceLifetime.Scoped, authHeaderProviderService.Lifetime);
            Assert.NotNull(authHeaderProviderService.ImplementationFactory);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            Assert.IsType<DefaultAuthorizationHeaderBoundProvider>(resolvedProvider);
        }

        [Fact]
        public void EnableTokenBinding_WithExistingSingletonAuthorizationHeaderProvider_ReplacesWithSingletonBoundProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.AddSingleton<ITokenAcquisition, MockTokenAcquisition>();
            services.AddSingleton<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();

            // Act
            services.EnableTokenBinding();

            // Assert
            ServiceDescriptor[] authHeaderProviderServices = services.Where(s => s.ServiceType == typeof(IAuthorizationHeaderProvider)).ToArray();
            Assert.Single(authHeaderProviderServices); // Should still be only one registration

            ServiceDescriptor authHeaderProviderService = authHeaderProviderServices[0];
            Assert.Equal(ServiceLifetime.Singleton, authHeaderProviderService.Lifetime); // Should preserve singleton lifetime
            Assert.NotNull(authHeaderProviderService.ImplementationFactory);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            Assert.IsType<DefaultAuthorizationHeaderBoundProvider>(resolvedProvider);
        }

        [Fact]
        public void EnableTokenBinding_WithCustomAuthorizationHeaderProvider_WrapsInBoundProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.AddSingleton<ITokenAcquisition, MockTokenAcquisition>();
            services.AddScoped<IAuthorizationHeaderProvider, MockAuthorizationHeaderProvider>();

            // Act
            services.EnableTokenBinding();

            // Assert
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            Assert.IsType<DefaultAuthorizationHeaderBoundProvider>(resolvedProvider);

            // Verify the bound provider can delegate to the underlying provider
            var task = resolvedProvider.CreateAuthorizationHeaderAsync(new[] { "scope" });
            Assert.NotNull(task);
        }

        [Fact]
        public void EnableTokenBinding_CalledMultipleTimes_KeepsSingleAuthorizationHeaderProviderRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.AddSingleton<ITokenAcquisition, MockTokenAcquisition>();
            services.AddScoped<IAuthorizationHeaderProvider, DefaultAuthorizationHeaderProvider>();

            // Act
            services.EnableTokenBinding();
            services.EnableTokenBinding(); // Call again

            // Assert
            ServiceDescriptor[] authHeaderProviderServices = services.Where(s => s.ServiceType == typeof(IAuthorizationHeaderProvider)).ToArray();
            Assert.Single(authHeaderProviderServices); // Should still be only one registration

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            Assert.IsType<DefaultAuthorizationHeaderBoundProvider>(resolvedProvider);
        }

        [Fact]
        public void EnableTokenBinding_WithoutITokenAcquisition_ThrowsWhenResolvingProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.EnableTokenBinding(); // Add token binding without ITokenAcquisition

            // Act & Assert
            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // The provider should be registered, but resolving it should throw because ITokenAcquisition is missing
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>());
        }

        [Fact]
        public async Task EnableTokenBinding_BoundProviderDelegatesToUnderlyingProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IHttpClientFactory, MockHttpClientFactory>();
            services.AddSingleton<ITokenAcquisition, MockTokenAcquisition>();
            services.AddScoped<IAuthorizationHeaderProvider, MockAuthorizationHeaderProvider>();

            services.EnableTokenBinding();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var resolvedProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            // Act
            string result = await resolvedProvider.CreateAuthorizationHeaderAsync(new[] { "scope" });

            // Assert
            Assert.Equal("MockAuthorizationHeader", result); // Should delegate to MockAuthorizationHeaderProvider
        }

        /// <summary>
        /// Mock HttpClientFactory for testing purposes.
        /// </summary>
        private sealed class MockHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient();
            }
        }

        /// <summary>
        /// Mock MSAL HttpClientFactory for testing purposes.
        /// </summary>
        private sealed class MockMsalHttpClientFactory : IMsalHttpClientFactory
        {
            public HttpClient GetHttpClient()
            {
                return new HttpClient();
            }
        }

        /// <summary>
        /// Mock TokenAcquisition service for testing purposes.
        /// </summary>
        private sealed class MockTokenAcquisition : ITokenAcquisition
        {
            public Task<string> GetAccessTokenForAppAsync(string scope, string? authenticationScheme, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            {
                return Task.FromResult("MockAppToken");
            }

            public Task<string> GetAccessTokenForUserAsync(IEnumerable<string> scopes, string? authenticationScheme, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            {
                return Task.FromResult("MockUserToken");
            }

            public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(string scopes, string? authenticationOptionsName = null, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new AuthenticationResult("MockAppToken", false, null, DateTimeOffset.Now, DateTimeOffset.Now, null, null, null, null, Guid.Empty));
            }

            public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(string scope, string? authenticationScheme, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            {
                return Task.FromResult(new AuthenticationResult("MockAppToken", false, null, DateTimeOffset.Now, DateTimeOffset.Now, null, null, null, null, Guid.Empty));
            }

            public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(IEnumerable<string> scopes, string? authenticationOptionsName = null, string? tenant = null, string? userFlow = null, ClaimsPrincipal? claimsPrincipal = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new AuthenticationResult("MockUserToken", false, null, DateTimeOffset.Now, DateTimeOffset.Now, null, null, null, null, Guid.Empty));
            }

            public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(IEnumerable<string> scopes, string? authenticationScheme, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            {
                return Task.FromResult(new AuthenticationResult("MockUserToken", false, null, DateTimeOffset.Now, DateTimeOffset.Now, null, null, null, null, Guid.Empty));
            }

            public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
            {
                return authenticationScheme ?? "Bearer";
            }

            public void ReplyForbiddenWithWwwAuthenticateHeader(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, string? authenticationScheme, HttpResponse? httpResponse = null)
            {
                // Mock implementation
            }

            public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, HttpResponse? httpResponse = null)
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Mock AuthorizationHeaderProvider for testing purposes.
        /// </summary>
        private sealed class MockAuthorizationHeaderProvider : IAuthorizationHeaderProvider
        {
            public Task<string> CreateAuthorizationHeaderAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? options = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult("MockAuthorizationHeader");
            }

            public Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, AuthorizationHeaderProviderOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult("MockAuthorizationHeaderForApp");
            }

            public Task<string> CreateAuthorizationHeaderForUserAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult("MockAuthorizationHeaderForUser");
            }
        }
    }

    internal class MockCredentialsLoader : ICredentialsLoader
    {
        public IDictionary<CredentialSource, ICredentialSourceLoader> CredentialSourceLoaders => throw new NotImplementedException();

        public Task LoadCredentialsIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters = null)
        {
            throw new NotImplementedException();
        }

        public Task<CredentialDescription?> LoadFirstValidCredentialsAsync(IEnumerable<CredentialDescription> credentialDescriptions, CredentialSourceLoaderParameters? parameters = null)
        {
            throw new NotImplementedException();
        }

        public void ResetCredentials(IEnumerable<CredentialDescription> credentialDescriptions)
        {
            throw new NotImplementedException();
        }
    }
}
