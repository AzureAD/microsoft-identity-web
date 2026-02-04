// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Tests for the AgentIdentityExtension class (ForAgentIdentity and WithAgentIdentity methods).
    /// </summary>
    public class AgentIdentitiesExtensionTests
    {
        private const string TestAgentApplicationId = "test-agent-app-id";

        [Fact]
        public void WithAgentIdentity_WithDefaultAuthenticationOptionsName_UsesAzureAdConfigurationSection()
        {
            // Arrange
            var options = new AuthorizationHeaderProviderOptions();

            // Act
            options.WithAgentIdentity(TestAgentApplicationId);

            // Assert
            Assert.NotNull(options.AcquireTokenOptions);
            Assert.NotNull(options.AcquireTokenOptions.ExtraParameters);
            Assert.True(options.AcquireTokenOptions.ExtraParameters.ContainsKey(Constants.MicrosoftIdentityOptionsParameter));

            var microsoftIdentityOptions = options.AcquireTokenOptions.ExtraParameters[Constants.MicrosoftIdentityOptionsParameter] as MicrosoftEntraApplicationOptions;
            Assert.NotNull(microsoftIdentityOptions);
            Assert.Equal(TestAgentApplicationId, microsoftIdentityOptions.ClientId);

            // Verify the ConfigurationSection is set to "AzureAd" when AuthenticationOptionsName is not set
            var clientCredential = Assert.Single(microsoftIdentityOptions.ClientCredentials!);
            Assert.Equal(CredentialSource.CustomSignedAssertion, clientCredential.SourceType);
            Assert.Equal("OidcIdpSignedAssertion", clientCredential.CustomSignedAssertionProviderName);
            Assert.NotNull(clientCredential.CustomSignedAssertionProviderData);
            Assert.True(clientCredential.CustomSignedAssertionProviderData.TryGetValue("ConfigurationSection", out var configSection));
            Assert.Equal("AzureAd", configSection);
        }

        [Fact]
        public void WithAgentIdentity_WithCustomAuthenticationOptionsName_UsesCustomConfigurationSection()
        {
            // Arrange
            var options = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = "MyEntraId"
                }
            };

            // Act
            options.WithAgentIdentity(TestAgentApplicationId);

            // Assert
            Assert.NotNull(options.AcquireTokenOptions);
            Assert.NotNull(options.AcquireTokenOptions.ExtraParameters);
            Assert.True(options.AcquireTokenOptions.ExtraParameters.ContainsKey(Constants.MicrosoftIdentityOptionsParameter));

            var microsoftIdentityOptions = options.AcquireTokenOptions.ExtraParameters[Constants.MicrosoftIdentityOptionsParameter] as MicrosoftEntraApplicationOptions;
            Assert.NotNull(microsoftIdentityOptions);

            // Verify the ConfigurationSection respects the custom AuthenticationOptionsName
            var clientCredential = Assert.Single(microsoftIdentityOptions.ClientCredentials!);
            Assert.NotNull(clientCredential.CustomSignedAssertionProviderData);
            Assert.True(clientCredential.CustomSignedAssertionProviderData.TryGetValue("ConfigurationSection", out var configSection));
            Assert.Equal("MyEntraId", configSection);
        }

        [Theory]
        [InlineData("EntraId")]
        [InlineData("CustomSection")]
        [InlineData("AzureAD_Prod")]
        public void WithAgentIdentity_WithVariousCustomAuthenticationOptionsNames_UsesCorrectConfigurationSection(string authenticationOptionsName)
        {
            // Arrange
            var options = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = authenticationOptionsName
                }
            };

            // Act
            options.WithAgentIdentity(TestAgentApplicationId);

            // Assert
            var microsoftIdentityOptions = options.AcquireTokenOptions.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter] as MicrosoftEntraApplicationOptions;
            Assert.NotNull(microsoftIdentityOptions);

            var clientCredential = Assert.Single(microsoftIdentityOptions.ClientCredentials!);
            Assert.True(clientCredential.CustomSignedAssertionProviderData!.TryGetValue("ConfigurationSection", out var configSection));
            Assert.Equal(authenticationOptionsName, configSection);
        }

        [Fact]
        public void WithAgentIdentity_WithNullOptions_CreatesNewOptionsAndUsesAzureAdDefault()
        {
            // Arrange
            AuthorizationHeaderProviderOptions? options = null;

            // Act
            var result = options!.WithAgentIdentity(TestAgentApplicationId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AcquireTokenOptions);
            Assert.NotNull(result.AcquireTokenOptions.ExtraParameters);

            var microsoftIdentityOptions = result.AcquireTokenOptions.ExtraParameters[Constants.MicrosoftIdentityOptionsParameter] as MicrosoftEntraApplicationOptions;
            Assert.NotNull(microsoftIdentityOptions);

            var clientCredential = Assert.Single(microsoftIdentityOptions.ClientCredentials!);
            Assert.True(clientCredential.CustomSignedAssertionProviderData!.TryGetValue("ConfigurationSection", out var configSection));
            Assert.Equal("AzureAd", configSection);
        }

        [Fact]
        public void WithAgentIdentity_AlwaysSetsRequiresSignedAssertionFmiPath()
        {
            // Arrange
            var options = new AuthorizationHeaderProviderOptions
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = "CustomSection"
                }
            };

            // Act
            options.WithAgentIdentity(TestAgentApplicationId);

            // Assert
            var microsoftIdentityOptions = options.AcquireTokenOptions.ExtraParameters![Constants.MicrosoftIdentityOptionsParameter] as MicrosoftEntraApplicationOptions;
            var clientCredential = Assert.Single(microsoftIdentityOptions!.ClientCredentials!);
            Assert.True(clientCredential.CustomSignedAssertionProviderData!.TryGetValue("RequiresSignedAssertionFmiPath", out var fmiPathRequired));
            Assert.Equal(true, fmiPathRequired);
        }
    }
}
