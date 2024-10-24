// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common;
using Xunit;
using System.Threading.Tasks;
using NSubstitute;

namespace Microsoft.Identity.Web.Tests
{
    public class TokenAcquisitionAddInTests
    {
        [Fact]
        public void InvokeOnBuildConfidentialClientApplication_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            var acquireTokenOptions = new AcquireTokenOptions();
            ConfidentialClientApplicationBuilder builderMock = null!;

            bool eventInvoked = false;
            options.OnBuildConfidentialClientApplication += (builder, options) =>
            {
                eventInvoked = true;
            };

            // Act
            options.InvokeOnBuildConfidentialClientApplication(builderMock, acquireTokenOptions);

            // Assert
            Assert.True(eventInvoked);
        }

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
        public void InvokeOnAfterTokenAcquisition_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            ConfidentialClientApplicationBuilder builderMock = null!;
            var resultMock = Substitute.For<AuthenticationResult>();
            var acquireTokenOptions = new AcquireTokenOptions();

            bool eventInvoked = false;
            options.OnAfterTokenAcquisition += (result, options) =>
            {
                eventInvoked = true;
            };

            // Act
            options.InvokeOnAfterTokenAcquisition(resultMock, acquireTokenOptions);

            // Assert
            Assert.True(eventInvoked);
        }
    }
}
