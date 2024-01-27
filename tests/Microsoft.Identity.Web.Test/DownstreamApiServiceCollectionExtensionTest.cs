// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Test
{
    using Xunit;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Identity.Web;
    using Microsoft.Identity.Abstractions;
    using System.Security.Claims;
    using Microsoft.Identity.Client;
    using Microsoft.AspNetCore.Http;

    namespace DownstreamApiTests
    {

        public class DownstreamApiTests
        {
            [Fact]
            public void RegisterDownstreamApi_WhenTokenAcquisitionServiceIsNull_AddsScopedDownstreamApi()
            {
                // Arrange
                var services = new ServiceCollection();

                // Act
                DownstreamApiExtensions.RegisterDownstreamApi(services);

                // Assert
                var downstreamApi = services.FirstOrDefault(s => s.ServiceType == typeof(IDownstreamApi));
                Assert.NotNull(downstreamApi);
                Assert.Equal(ServiceLifetime.Scoped, downstreamApi.Lifetime);
            }

            [Fact]
            public void RegisterDownstreamApi_WhenTokenAcquisitionServiceIsNotNullAndDownstreamApiIsNull_AddsDownstreamApiWithSameLifetime()
            {
                // Arrange
                var services = new ServiceCollection();
                services.AddSingleton<ITokenAcquisition, TokenAcquisitionTest>();

                // Act
                DownstreamApiExtensions.RegisterDownstreamApi(services);

                // Assert
                var downstreamApi = services.FirstOrDefault(s => s.ServiceType == typeof(IDownstreamApi));
                Assert.NotNull(downstreamApi);
                Assert.Equal(ServiceLifetime.Singleton, downstreamApi.Lifetime);
            }

            [Fact]
            public void RegisterDownstreamApi_WhenTokenAcquisitionServiceIsNotNullAndDownstreamApiIsNotNullAndLifetimesAreDifferent_ReplacesDownstreamApiWithSameLifetime()
            {
                // Arrange
                var services = new ServiceCollection();
                services.AddSingleton<ITokenAcquisition, TokenAcquisitionTest>();
                services.AddScoped<IDownstreamApi, DownstreamApi>();

                // Act
                DownstreamApiExtensions.RegisterDownstreamApi(services);

                // Assert
                var downstreamApi = services.FirstOrDefault(s => s.ServiceType == typeof(IDownstreamApi));
                Assert.NotNull(downstreamApi);
                Assert.Equal(ServiceLifetime.Singleton, downstreamApi.Lifetime);
            }

            [Fact]
            public void RegisterDownstreamApi_WhenTokenAcquisitionServiceIsNotNullAndDownstreamApiIsNotNullAndLifetimesAreSame_DoesNotChangeDownstreamApi()
            {
                // Arrange
                var services = new ServiceCollection();
                services.AddSingleton<ITokenAcquisition, TokenAcquisitionTest>();
                services.AddSingleton<IDownstreamApi, DownstreamApi>();

                // Act
                DownstreamApiExtensions.RegisterDownstreamApi(services);

                // Assert
                var downstreamApi = services.FirstOrDefault(s => s.ServiceType == typeof(IDownstreamApi));
                Assert.NotNull(downstreamApi);
                Assert.Equal(ServiceLifetime.Singleton, downstreamApi.Lifetime);
            }

            [Fact]
            public void AddDownstreamApiWithLifetime_WhenLifetimeIsSingleton_AddsSingletonDownstreamApi()
            {
                // Arrange
                var services = new ServiceCollection();

                // Act
                DownstreamApiExtensions.AddDownstreamApiWithLifetime(services, ServiceLifetime.Singleton);

                // Assert
                var downstreamApi = services.FirstOrDefault(s => s.ServiceType == typeof(IDownstreamApi));
                Assert.NotNull(downstreamApi);
                Assert.Equal(ServiceLifetime.Singleton, downstreamApi.Lifetime);
            }

            [Fact]
            public void AddDownstreamApiWithLifetime_WhenLifetimeIsScoped_AddsScopedDownstreamApi()
            {
                // Arrange
                var services = new ServiceCollection();

                // Act
                DownstreamApiExtensions.AddDownstreamApiWithLifetime(services, ServiceLifetime.Scoped);

                // Assert
                var downstreamApi = services.FirstOrDefault(s => s.ServiceType == typeof(IDownstreamApi));
                Assert.NotNull(downstreamApi);
                Assert.Equal(ServiceLifetime.Scoped, downstreamApi.Lifetime);
            }

            class TokenAcquisitionTest : ITokenAcquisition
            {
                public Task<string> GetAccessTokenForAppAsync(string scope, string? authenticationScheme, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
                {
                    throw new NotImplementedException();
                }

                public Task<string> GetAccessTokenForUserAsync(IEnumerable<string> scopes, string? authenticationScheme, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
                {
                    throw new NotImplementedException();
                }

                public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(string scope, string? authenticationScheme, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
                {
                    throw new NotImplementedException();
                }

                public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(IEnumerable<string> scopes, string? authenticationScheme, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
                {
                    throw new NotImplementedException();
                }

                public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
                {
                    throw new NotImplementedException();
                }

                public void ReplyForbiddenWithWwwAuthenticateHeader(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, string? authenticationScheme, HttpResponse? httpResponse = null)
                {
                    throw new NotImplementedException();
                }

                public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(IEnumerable<string> scopes, MsalUiRequiredException msalServiceException, HttpResponse? httpResponse = null)
                {
                    throw new NotImplementedException();
                }
            }

        }
    }
}
