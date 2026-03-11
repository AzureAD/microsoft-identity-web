// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Graph;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Identity.Client.AuthScheme;
using System.Threading;
using System;

namespace Microsoft.Identity.Web.Tests
{
    public class TokenAcquisitionAddInTests
    {
        [Fact]
        public async Task InvokeOnBeforeTokenAcquisitionForApp_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            var acquireTokenOptions = new AcquireTokenOptions();
            acquireTokenOptions.ForceRefresh = true;

            //Configure mocks
            using MockHttpClientFactory mockHttpClient = new();
            mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());
            mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());

            var confidentialApp = ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithHttpClientFactory(mockHttpClient)
                           .WithInstanceDiscovery(false)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();

            AcquireTokenForClientParameterBuilder builder = confidentialApp.AcquireTokenForClient(new string[] { "scope" });

            //Populate Cache
            var result = await builder.ExecuteAsync();
            Assert.NotNull(result);
            Assert.True(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);

            bool eventInvoked = false;
            options.OnBeforeTokenAcquisitionForApp += (builder, options) =>
            {
                eventInvoked = true;

                //Set ForceRefresh on the builder
                builder.WithForceRefresh(options!.ForceRefresh);
            };

            // Act
            options.InvokeOnBeforeTokenAcquisitionForApp(builder, acquireTokenOptions);

            //Ensure ForceRefresh is set on the builder
            result = await builder.ExecuteAsync();

            // Assert
            Assert.True(eventInvoked);
            Assert.NotNull(result);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        [Fact]
        public async Task InvokeOnBeforeTokenAcquisitionForUsernamePassword_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            var acquireTokenOptions = new AcquireTokenOptions();

            //Configure mocks
            using MockHttpClientFactory mockHttpClient = new();
            mockHttpClient.AddMockHandler(MockHttpCreator.CreateHandlerToValidatePostData(
                System.Net.Http.HttpMethod.Post,
                new Dictionary<string, string>() { { "x-ms-user", "username" } }));

            var confidentialApp = ConfidentialClientApplicationBuilder
                           .Create(TestConstants.ClientId)
                           .WithAuthority(TestConstants.AuthorityCommonTenant)
                           .WithHttpClientFactory(mockHttpClient)
                           .WithInstanceDiscovery(false)
                           .WithClientSecret(TestConstants.ClientSecret)
                           .Build();

            AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder = ((IByUsernameAndPassword)confidentialApp)
                .AcquireTokenByUsernamePassword(new string[] { "scope" }, "username", "something");

            bool eventInvoked = false;
            options.OnBeforeTokenAcquisitionForTestUser += (builder, options, user) =>
            {
                MsalAuthenticationExtension extension = new MsalAuthenticationExtension();
                extension.OnBeforeTokenRequestHandler = (request) =>
                {
                    eventInvoked = true;
                    request.BodyParameters.Add("x-ms-user", user.FindFirst("user")?.Value);
                    return Task.CompletedTask;
                };

                builder.WithAuthenticationExtension(extension);
            };

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new[]
                {
                    new Claim("user", "username"),
                    new Claim("assertion", "assertion"),
                }));

            // Act
            await options.InvokeOnBeforeTokenAcquisitionForTestUserAsync(builder, acquireTokenOptions, user);

            var result = await builder.ExecuteAsync();

            // Assert
            Assert.True(eventInvoked);
            Assert.NotNull(result);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        [Fact]
        public async Task InvokeOnBeforeTokenAcquisitionForOnBehalfOf_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            var acquireTokenOptions = new AcquireTokenOptions();
            acquireTokenOptions.ForceRefresh = true;

            //Configure mocks
            using MockHttpClientFactory mockHttpClient = new();
            mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler());

            var confidentialApp = ConfidentialClientApplicationBuilder
                   .Create(TestConstants.ClientId)
                   .WithAuthority(TestConstants.AuthorityCommonTenant)
                   .WithHttpClientFactory(mockHttpClient)
                   .WithInstanceDiscovery(false)
                   .WithClientSecret(TestConstants.ClientSecret)
                   .WithExperimentalFeatures(true)
                   .Build();

            var userAssertion = new UserAssertion("user-assertion-token");
            AcquireTokenOnBehalfOfParameterBuilder builder = confidentialApp
                .AcquireTokenOnBehalfOf(new string[] { "scope" }, userAssertion);

            bool eventInvoked = false;
            bool formatResultInvoked = false;
            
            MsalAuthenticationExtension extension = new MsalAuthenticationExtension();
            options.OnBeforeTokenAcquisitionForOnBehalfOf += (builder, options, user) =>
            {
                MsalAuthenticationExtension extension = new MsalAuthenticationExtension();

                // Create a test authentication operation implementing IAuthenticationOperation2
                var authOperation = new TestAuthenticationOperation2
                {
                    OnFormatResult = (result) =>
                    {
                        formatResultInvoked = true;
                        return Task.FromResult(result);
                    }
                };
                extension.AuthenticationOperation = authOperation;
                extension.OnBeforeTokenRequestHandler = (request) =>
                {
                    eventInvoked = true;
                    request.BodyParameters.Add("x-ms-user", user?.User?.FindFirst("user")?.Value);
                    return Task.CompletedTask;
                };

                builder.WithAuthenticationExtension(extension);
            };

            var user = new ClaimsPrincipal(
                new CaseSensitiveClaimsIdentity(new[]
                {
                    new Claim(ClaimConstants.Sub, "user-id"),
                    new Claim(ClaimConstants.Name, "Test User"),
                }));

            // Act
            var eventArgs = new OnBehalfOfEventArgs() { User = user };
            await options.InvokeOnBeforeTokenAcquisitionForOnBehalfOfAsync(builder, acquireTokenOptions, eventArgs);

            var result = await builder.ExecuteAsync();

            // Assert
            Assert.True(eventInvoked);
            Assert.True(formatResultInvoked);
            Assert.NotNull(result);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }

        [Fact]
        public async Task InvokeOnBeforeOnBehalfOfInitializedAsync_SyncHandler_CanChangeUserAssertionToken()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            string originalAssertion = "original-assertion";
            string modifiedAssertion = "modified-assertion";

            bool eventInvoked = false;
            options.OnBeforeOnBehalfOfInitialized += (eventArgs) =>
            {
                eventInvoked = true;
                Assert.Equal(originalAssertion, eventArgs.UserAssertionToken);
                eventArgs.UserAssertionToken = modifiedAssertion;
            };

            var eventArgsObj = new OnBehalfOfEventArgs
            {
                UserAssertionToken = originalAssertion
            };

            // Act
            await options.InvokeOnBeforeOnBehalfOfInitializedAsync(eventArgsObj);

            // Assert
            Assert.True(eventInvoked);
            Assert.Equal(modifiedAssertion, eventArgsObj.UserAssertionToken);
        }

        [Fact]
        public async Task InvokeOnBeforeOnBehalfOfInitializedAsync_AsyncHandler_CanChangeUserAssertionToken()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            string originalAssertion = "original-assertion";
            string modifiedAssertion = "modified-assertion-async";

            bool eventInvoked = false;
            options.OnBeforeOnBehalfOfInitializedAsync += (eventArgs) =>
            {
                eventInvoked = true;
                Assert.Equal(originalAssertion, eventArgs.UserAssertionToken);
                eventArgs.UserAssertionToken = modifiedAssertion;
                return Task.CompletedTask;
            };

            var eventArgsObj = new OnBehalfOfEventArgs
            {
                UserAssertionToken = originalAssertion
            };

            // Act
            await options.InvokeOnBeforeOnBehalfOfInitializedAsync(eventArgsObj);

            // Assert
            Assert.True(eventInvoked);
            Assert.Equal(modifiedAssertion, eventArgsObj.UserAssertionToken);
        }

        // Helper class for testing IAuthenticationOperation2
        private class TestAuthenticationOperation2 : IAuthenticationOperation2
        {
            public Func<AuthenticationResult, Task<AuthenticationResult>>? OnFormatResult { get; set; }

            public int TelemetryTokenType => 0;

            public string AuthorizationHeaderPrefix => "Bearer";

            public string KeyId => string.Empty;

            public string AccessTokenType => "Bearer";

            public void FormatResult(AuthenticationResult authenticationResult) { }

            public Task FormatResultAsync(AuthenticationResult authenticationResult, CancellationToken cancellationToken = default)
            {
                if (OnFormatResult != null)
                {
                    return OnFormatResult(authenticationResult);
                }
                return Task.FromResult(authenticationResult);
            }

            public IReadOnlyDictionary<string, string> GetTokenRequestParams() => new Dictionary<string, string>();
            
            public Task<bool> ValidateCachedTokenAsync(MsalCacheValidationData cachedTokenData) => Task.FromResult(false);
        }
    }
}
