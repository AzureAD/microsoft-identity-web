using System;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.Test.Common;
using NSubstitute;
using Xunit;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Tests
{
    public class TokenAcquisitionAddInTests
    {
#if FUTURE
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
#endif
        [Fact]
        public async Task InvokeOnBeforeTokenAcquisitionForApp_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            var acquireTokenOptions = new AcquireTokenOptions();
            acquireTokenOptions.ForceRefresh = true;

            //Configure mocks
            using MockHttpClientFactory mockHttpClient = new MockHttpClientFactory();
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler()))
            using (mockHttpClient.AddMockHandler(MockHttpCreator.CreateClientCredentialTokenHandler()))
            {
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
                options.OnBeforeTokenAcquisitionForApp += async (builder, options) =>
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
                Assert.True(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);
            }
        }

#if FUTURE
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
#endif
    }
}
