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
            acquireTokenOptions.ForceRefresh = true;

            //Configure mocks
            using MockHttpClientFactory mockHttpClient = new();
            mockHttpClient.AddMockHandler(MockHttpCreator.CreateHandlerToValidatePostData(
                System.Net.Http.HttpMethod.Post,
                new Dictionary<string, string>() { { "x-ms-test", "test" } }));

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
            options.OnBeforeTokenAcquisitionForTestUser += (builder, options) =>
            {
                MsalAuthenticationExtension extension = new MsalAuthenticationExtension();
                extension.OnBeforeTokenRequestHandler = (request) =>
                {
                    eventInvoked = true;
                    request.BodyParameters.Add("x-ms-test", "test");
                    return Task.CompletedTask;
                };

                builder.WithAuthenticationExtension(extension);
            };

            // Act
            options.InvokeOnBeforeTokenAcquisitionForTestUser(builder, acquireTokenOptions);

            var result = await builder.ExecuteAsync();

            // Assert
            Assert.True(eventInvoked);
            Assert.NotNull(result);
            Assert.Equal(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
        }
    }
}
