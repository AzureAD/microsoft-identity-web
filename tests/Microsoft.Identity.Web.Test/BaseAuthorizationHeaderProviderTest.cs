// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Extensibility;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class BaseAuthorizationHeaderProviderTest
    {
        public BaseAuthorizationHeaderProviderTest()
        {
            TokenAcquirerFactory.ResetDefaultInstance(); // Test only
        }

        // Example of extension
        class CustomAuthorizationHeaderProvider : BaseAuthorizationHeaderProvider
        {
            public CustomAuthorizationHeaderProvider(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            public override Task<string> CreateAuthorizationHeaderForAppAsync(string scopes, AuthorizationHeaderProviderOptions? downstreamApiOptions = null, CancellationToken cancellationToken = default)
            {
                if (downstreamApiOptions?.ProtocolScheme == "Custom")
                    return Task.FromResult("CustomHeaderForApp");
                else
                    return base.CreateAuthorizationHeaderForAppAsync(scopes, downstreamApiOptions, cancellationToken);
            }

            public override Task<string> CreateAuthorizationHeaderForUserAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
            {
                if (authorizationHeaderProviderOptions?.ProtocolScheme == "Custom")
                    return Task.FromResult("CustomHeaderForUser");
                else
                    return base.CreateAuthorizationHeaderForUserAsync(scopes, authorizationHeaderProviderOptions, claimsPrincipal, cancellationToken);
            }

            public override Task<string> CreateAuthorizationHeaderAsync(IEnumerable<string> scopes, AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, ClaimsPrincipal? claimsPrincipal = null, CancellationToken cancellationToken = default)
            {
                if (claimsPrincipal == null && authorizationHeaderProviderOptions?.ProtocolScheme == "Custom")
                    return Task.FromResult("CustomHeaderForApp");
                else if (claimsPrincipal != null && authorizationHeaderProviderOptions?.ProtocolScheme == "Custom")
                    return Task.FromResult("CustomHeaderForUser");
                else
                    return base.CreateAuthorizationHeaderAsync(scopes, authorizationHeaderProviderOptions, claimsPrincipal, cancellationToken);
            }
        }

        // Mock for ITokenAcquisition
        class CustomTokenAcquisition : ITokenAcquisition
        {
            public Task<string> GetAccessTokenForAppAsync(string scope, string? authenticationScheme, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            {
                throw new NotImplementedException();
            }

            public Task<string> GetAccessTokenForUserAsync(IEnumerable<string> scopes, string? authenticationScheme, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            {
                throw new NotImplementedException();
            }

            public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(string scopes, string? authenticationOptionsName = null, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(string scope, string? authenticationScheme, string? tenant = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            {
                throw new NotImplementedException();
            }

            public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(IEnumerable<string> scopes, string? authenticationOptionsName = null, string? tenant = null, string? userFlow = null, ClaimsPrincipal? claimsPrincipal = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(IEnumerable<string> scopes, string? authenticationScheme, string? tenantId = null, string? userFlow = null, ClaimsPrincipal? user = null, TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            {
                return Task.FromResult(new AuthenticationResult("eXY", false, null, DateTimeOffset.Now, DateTimeOffset.Now, null, null, null, null, Guid.Empty));
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

        [Fact]
        public async Task TestBaseAuthorizationHeaderProviderAsync()
        {
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            // Test the extensibility
            tokenAcquirerFactory.Services.AddSingleton<IAuthorizationHeaderProvider, CustomAuthorizationHeaderProvider>();

            // Mock the token acquisition
            tokenAcquirerFactory.Services.AddSingleton<ITokenAcquisition, CustomTokenAcquisition>();
            var serviceProvider = tokenAcquirerFactory.Build();

            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
            
            // test acquiring a header on behalf of a user
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(["scope"],
                new AuthorizationHeaderProviderOptions { ProtocolScheme = "Custom" }, new ClaimsPrincipal(), CancellationToken.None);
            Assert.Equal("CustomHeaderForUser", result);

            result = await authorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(["scope"],
                new AuthorizationHeaderProviderOptions { }, new ClaimsPrincipal(), CancellationToken.None);
            Assert.Equal("Bearer eXY", result);

            result = await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(["scope"],
                new AuthorizationHeaderProviderOptions { ProtocolScheme = "Custom" }, new ClaimsPrincipal(), CancellationToken.None);
            Assert.Equal("CustomHeaderForUser", result);

            TokenAcquirerFactory.ResetDefaultInstance(); // Test only

            // test acquiring a header on behalf of an app
            result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("scope",
                new AuthorizationHeaderProviderOptions { ProtocolScheme = "Custom" }, CancellationToken.None);
            Assert.Equal("CustomHeaderForApp", result);

            result = await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(["scope"],
                new AuthorizationHeaderProviderOptions { ProtocolScheme = "Custom" }, null, CancellationToken.None);
            Assert.Equal("CustomHeaderForApp", result);

            TokenAcquirerFactory.ResetDefaultInstance(); // Test only
        }
    }
}
