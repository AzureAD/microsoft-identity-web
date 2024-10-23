using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using NSubstitute;
using Xunit;

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
        public void InvokeOnBeforeTokenAcquisitionForApp_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionExtensionOptions();
            var acquireTokenOptions = new AcquireTokenOptions();
            acquireTokenOptions.ForceRefresh = true;
            AcquireTokenForClientParameterBuilder builderMock = null!;

            bool eventInvoked = false;
            options.OnBeforeTokenAcquisitionForApp += (builder, options) =>
            {
                eventInvoked = true;
                builder.WithForceRefresh(options!.ForceRefresh);
                builder.Received().WithForceRefresh(true);
            };

            // Act
            options.InvokeOnBeforeTokenAcquisitionForApp(builderMock, acquireTokenOptions);

            // Assert
            Assert.True(eventInvoked);
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
