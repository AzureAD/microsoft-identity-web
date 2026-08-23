// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class KeyAttestationTests
    {
        [Fact]
        public void AddMicrosoftIdentityWebKeyAttestation_RegistersOptionalServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTokenAcquisition();

            // Act
            services.AddMicrosoftIdentityWebKeyAttestation();
            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<IManagedIdentityAttestationProvider>());
            Assert.Single(
                serviceProvider.GetServices<ICredentialSourceLoader>(),
                loader => loader.CredentialSource == CredentialSource.SignedAssertionFromManagedIdentity);

            var credentialsLoader = Assert.IsType<DefaultCertificateLoader>(
                serviceProvider.GetRequiredService<ICredentialsLoader>());
            Assert.Equal(
                typeof(KeyAttestationServiceCollectionExtensions).Assembly,
                credentialsLoader.CredentialSourceLoaders[CredentialSource.SignedAssertionFromManagedIdentity]
                    .GetType()
                    .Assembly);
        }

        [Fact]
        public async Task GetSignedAssertionWithBindingAsync_WithAttestationProvider_UsesProvider()
        {
            // Arrange
            var provider = new ThrowingAttestationProvider();
            var assertion = new ManagedIdentityClientAssertion(
                managedIdentityClientId: null,
                tokenExchangeUrl: null,
                logger: null,
                attestationProvider: provider);

            // Act
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => assertion.GetSignedAssertionWithBindingAsync(null));

            // Assert
            Assert.Equal(ThrowingAttestationProvider.ErrorMessage, exception.Message);
        }

        private sealed class ThrowingAttestationProvider : IManagedIdentityAttestationProvider
        {
            internal const string ErrorMessage = "Attestation provider invoked.";

            public AcquireTokenForManagedIdentityParameterBuilder EnableAttestation(
                AcquireTokenForManagedIdentityParameterBuilder builder)
            {
                throw new InvalidOperationException(ErrorMessage);
            }
        }
    }
}
