// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test.Certificates
{
    public class DefaultCredentialsLoaderSigningCredentialsTests
    {
        private readonly CustomMockLogger<DefaultCredentialsLoader> _logger;
        private readonly DefaultCredentialsLoader _loader;

        public DefaultCredentialsLoaderSigningCredentialsTests()
        {
            _logger = new CustomMockLogger<DefaultCredentialsLoader>();
            _loader = new DefaultCredentialsLoader(_logger);
        }

        [Fact]
        public async Task LoadSigningCredentialsAsync_NullDescription_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _loader.LoadSigningCredentialsAsync(null!, null));
        }

        [Theory]
        [MemberData(nameof(LoadSigningCredentialsTheoryData))]
        public async Task LoadSigningCredentialsAsync_Tests(SigningCredentialsTheoryData data)
        {
            // Act
            if (data.ExpectedException != null)
            {
                var exception = await Assert.ThrowsAsync(
                    data.ExpectedException.GetType(),
                    () => _loader.LoadSigningCredentialsAsync(data.CredentialDescription, null));

                return;
            }

            var result = await _loader.LoadSigningCredentialsAsync(data.CredentialDescription, null);

            // Assert
            if (data.ExpectNullResult)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.NotNull(result);
                Assert.IsType<X509SigningCredentials>(result);
                Assert.Equal(data.CredentialDescription.Algorithm, result.Algorithm);
                Assert.NotNull(((X509SigningCredentials)result).Certificate);
            }
        }

        public static TheoryData<SigningCredentialsTheoryData> LoadSigningCredentialsTheoryData()
        {
            return new TheoryData<SigningCredentialsTheoryData>
            {
                // Test with Base64 encoded certificate with private key
                new SigningCredentialsTheoryData
                {
                    CredentialDescription = CertificateDescription.FromBase64Encoded(
                        TestConstants.CertificateX5cWithPrivateKey,
                        TestConstants.CertificateX5cWithPrivateKeyPassword),
                    ExpectNullResult = false,
                    Algorithm = SecurityAlgorithms.RsaSha512
                },

                // Test with certificate from file
                new SigningCredentialsTheoryData
                {
                    CredentialDescription = CertificateDescription.FromPath(
                        "Certificates/SelfSignedTestCert.pfx",
                        TestConstants.CertificateX5cWithPrivateKeyPassword),
                    ExpectNullResult = false,
                    Algorithm = SecurityAlgorithms.RsaSha384
                },

                // Test with invalid certificate description
                new SigningCredentialsTheoryData
                {
                    CredentialDescription = new CertificateDescription(),
                    ExpectNullResult = true,
                    Algorithm = SecurityAlgorithms.RsaSha256
                },

                // Test with invalid Base64 value (should throw)
                new SigningCredentialsTheoryData
                {
                    CredentialDescription = CertificateDescription.FromBase64Encoded("invalid"),
                    ExpectNullResult = false,
                    ExpectedException = new FormatException(),
                    Algorithm = SecurityAlgorithms.RsaSha256
                },

                // Test with invalid file path (should throw)
                new SigningCredentialsTheoryData
                {
                    CredentialDescription = CertificateDescription.FromPath(
                        "nonexistent.pfx",
                        TestConstants.CertificateX5cWithPrivateKeyPassword),
                    ExpectNullResult = false,
                    ExpectedException = new System.Security.Cryptography.CryptographicException(),
                    Algorithm = SecurityAlgorithms.RsaSha256
                }
            };
        }
    }

    public class SigningCredentialsTheoryData
    {
        public CertificateDescription CredentialDescription { get; set; } = new CertificateDescription();
        public bool ExpectNullResult { get; set; }
        public Exception? ExpectedException { get; set; }
        public string Algorithm
        {
            get => CredentialDescription.Algorithm!;
            set => CredentialDescription.Algorithm = value;
        }
    }
}
