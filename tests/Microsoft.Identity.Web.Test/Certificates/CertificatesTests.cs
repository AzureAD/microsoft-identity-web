// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common;
using Xunit;
using static Microsoft.Identity.Web.MicrosoftIdentityOptionsValidation;

namespace Microsoft.Identity.Web.Test.Certificates
{
    public class CertificatesTests
    {
        [Theory]
        [InlineData("some_secret")]
        public void ValidateCredentialType(string clientSecret)
        {
            // Arrange
            MicrosoftIdentityOptions microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ConfidentialClientId,
                ClientSecret = clientSecret,
            };

            // Act
            MicrosoftIdentityOptionsValidation microsoftIdentityOptionsValidation = new MicrosoftIdentityOptionsValidation();

            ClientCredentialType clientCredentialType =
                microsoftIdentityOptionsValidation.ValidateEitherClientCertificateOrClientSecret(microsoftIdentityOptions);

            // Assert
            Assert.Equal(ClientCredentialType.Secret, clientCredentialType);
        }

        [Theory]
        [InlineData("440A5BE6C4BE2FF02A0ADBED1AAA43D6CF12E269")]
        public void ValidateCredentialType_Certificate(string base64Encoded)
        {
            // Arrange
            CertificateDescription certificateDescription =
                    CertificateDescription.FromBase64Encoded(base64Encoded);

            MicrosoftIdentityOptions microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ConfidentialClientId,
                ClientCertificates = new CertificateDescription[] { certificateDescription },
            };

            // Act
            MicrosoftIdentityOptionsValidation microsoftIdentityOptionsValidation = new MicrosoftIdentityOptionsValidation();
            ClientCredentialType clientCredentialType =
                microsoftIdentityOptionsValidation.ValidateEitherClientCertificateOrClientSecret(microsoftIdentityOptions);

            // Assert
            Assert.Equal(ClientCredentialType.Certificate, clientCredentialType);
        }

        [Fact]
        public void NoCredentialTypesDefined_Throw()
        {
            // Arrange
            MicrosoftIdentityOptions microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ConfidentialClientId,
            };

            // Act
            MicrosoftIdentityOptionsValidation microsoftIdentityOptionsValidation = new MicrosoftIdentityOptionsValidation();

            Action credentialAction = () =>
            microsoftIdentityOptionsValidation.ValidateEitherClientCertificateOrClientSecret(microsoftIdentityOptions);

            // Assert
            var exception = Assert.Throws<MsalClientException>(credentialAction);

            Assert.Equal(
                string.Format(CultureInfo.InvariantCulture, "Both client secret & client certificate cannot be null or whitespace, " +
                 "and ONE, must be included in the configuration of the web app when calling a web API. " +
                 "For instance, in the appsettings.json file. "), exception.Message);
            Assert.Equal("missing_client_credentials", exception.ErrorCode);
        }

        [Fact]
        public void BothCredentialTypesDefined_Throw()
        {
            // Arrange
            CertificateDescription certificateDescription =
                    CertificateDescription.FromBase64Encoded("encoded");

            MicrosoftIdentityOptions microsoftIdentityOptions = new MicrosoftIdentityOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ConfidentialClientId,
                ClientSecret = "some secret",
                ClientCertificates = new CertificateDescription[] { certificateDescription },
            };

            // Act
            MicrosoftIdentityOptionsValidation microsoftIdentityOptionsValidation = new MicrosoftIdentityOptionsValidation();

            Action credentialAction = () =>
            microsoftIdentityOptionsValidation.ValidateEitherClientCertificateOrClientSecret(microsoftIdentityOptions);

            // Assert
            var exception = Assert.Throws<MsalClientException>(credentialAction);

            Assert.Equal(
                string.Format(CultureInfo.InvariantCulture, "Both Client secret & client certificate, " +
                   "cannot be included in the configuration of the web app when calling a web API. "), exception.Message);
            Assert.Equal("duplicate_client_credentials", exception.ErrorCode);
        }
    }
}
