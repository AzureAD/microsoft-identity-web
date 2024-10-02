using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Tests
{
    public class TokenAcquisitionAddInTests
    {
        [Fact]
        public void InvokeOnBuildConfidentialClientApplication_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionAddInOptions();
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
        public void InvokeOnBeforeTokenAcquisitionForApp_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionAddInOptions();
            var acquireTokenOptions = new AcquireTokenOptions();
            AcquireTokenForClientParameterBuilder builderMock = null!;

            bool eventInvoked = false;
            options.OnBeforeTokenAcquisitionForApp += (builder, options) =>
            {
                eventInvoked = true;
            };

            // Act
            options.InvokeOnBeforeTokenAcquisitionForApp(builderMock, acquireTokenOptions);

            // Assert
            Assert.True(eventInvoked);
        }

        [Fact]
        public void InvokeOnAfterTokenAcquisition_InvokesEvent()
        {
            // Arrange
            var options = new TokenAcquisitionAddInOptions();
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
