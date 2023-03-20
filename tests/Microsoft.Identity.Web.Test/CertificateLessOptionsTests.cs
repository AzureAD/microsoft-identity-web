// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CertificateLessOptionsTests
    {
        [Fact]
        public void IsEnabled_DefaultValue_IsFalse()
        {
            // Arrange
            var options = new CertificatelessOptions();

            // Act/Assert
            Assert.False(options.IsEnabled);
        }

        [Fact]
        public void IsEnabled_SetValue_GetsValue()
        {
            // Arrange
            var options = new CertificatelessOptions
            {
                IsEnabled = true
            };

            // Act/Assert
            Assert.True(options.IsEnabled);
        }

        [Fact]
        public void ManagedIdentityClientId_DefaultValue_IsNull()
        {
            // Arrange
            var options = new CertificatelessOptions();

            // Act/Assert
            Assert.Null(options.ManagedIdentityClientId);
        }

        [Fact]
        public void ManagedIdentityClientId_SetValue_GetsValue()
        {
            // Arrange
            var options = new CertificatelessOptions
            {
                ManagedIdentityClientId = "client_id"
            };

            // Act/Assert
            Assert.Equal("client_id", options.ManagedIdentityClientId);
        }
    }
}
