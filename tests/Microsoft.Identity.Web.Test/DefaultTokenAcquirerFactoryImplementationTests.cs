// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class DefaultTokenAcquirerFactoryImplementationTests
    {
        [Fact]
        public void GetTokenAcquirer_WithMicrosoftEntraApplicationOptions_PropagatesAllOptions()
        {
            // Arrange
            var taf = new CustomTAF();
            var provider = taf.Build();
            var factory = provider.GetRequiredService<ITokenAcquirerFactory>();

            var options = new MicrosoftEntraApplicationOptions
            {
                // IdentityApplicationOptions properties
                ClientId = "test-client-id",
                Authority = "https://login.microsoftonline.com/test-tenant",
                EnablePiiLogging = true,
                AllowWebApiToBeAuthorizedByACL = true,
                Audience = "test-audience",
                ClientCredentials = new List<CredentialDescription>
                {
                    new CredentialDescription { ClientSecret = "test-secret", SourceType = CredentialSource.ClientSecret }
                },

                // MicrosoftEntraApplicationOptions properties
                Name = "test-name",
                Instance = "https://login.microsoftonline.com/",
                TenantId = "test-tenant-id",
                AppHomeTenantId = "test-home-tenant-id",
                AzureRegion = "westus",
                ClientCapabilities = new List<string> { "cp1" },
                SendX5C = true
            };

            // Act
            var tokenAcquirer = factory.GetTokenAcquirer(options);

            // Assert
            Assert.NotNull(tokenAcquirer);
            
            // Verify the options were properly stored in the merged options
            var mergedOptionsStore = provider.GetRequiredService<IMergedOptionsStore>();
            var key = DefaultTokenAcquirerFactoryImplementation.GetKey(options.Authority, options.ClientId, options.AzureRegion);
            var mergedOptions = mergedOptionsStore.Get(key);

            Assert.Equal(options.ClientId, mergedOptions.ClientId);
            Assert.Equal(options.EnablePiiLogging, mergedOptions.EnablePiiLogging);
            Assert.Equal(options.AllowWebApiToBeAuthorizedByACL, mergedOptions.AllowWebApiToBeAuthorizedByACL);
            Assert.Equal(options.Instance, mergedOptions.Instance);
            Assert.Equal(options.TenantId, mergedOptions.TenantId);
            Assert.Equal(options.AppHomeTenantId, mergedOptions.AppHomeTenantId);
            Assert.Equal(options.AzureRegion, mergedOptions.AzureRegion);
            Assert.Equal(options.ClientCapabilities, mergedOptions.ClientCapabilities);
            Assert.Equal(options.SendX5C, mergedOptions.SendX5C);
            var namedOptions = provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(key);
            Assert.Equal(options.ClientId, namedOptions.ClientId);
            Assert.Equal(options.Authority, namedOptions.Authority);
            Assert.Equal(options.AzureRegion, namedOptions.AzureRegion);
            Assert.Same(options.ClientCredentials, namedOptions.ClientCredentials);
        }

        [Fact]
        public void GetTokenAcquirer_WithIdentityApplicationOptions_PropagatesBaseOptions()
        {
            // Arrange
            var taf = new CustomTAF();
            var provider = taf.Build();
            var factory = provider.GetRequiredService<ITokenAcquirerFactory>();

            var options = new IdentityApplicationOptions
            {
                ClientId = "test-client-id",
                Authority = "https://login.microsoftonline.com/test-tenant",
                EnablePiiLogging = true,
                AllowWebApiToBeAuthorizedByACL = true,
                Audience = "test-audience",
                ClientCredentials = new List<CredentialDescription>
                {
                    new CredentialDescription { ClientSecret = "test-secret", SourceType = CredentialSource.ClientSecret }
                }
            };

            // Act
            var tokenAcquirer = factory.GetTokenAcquirer(options);

            // Assert
            Assert.NotNull(tokenAcquirer);
            
            // Verify the options were properly stored in the merged options
            var mergedOptionsStore = provider.GetRequiredService<IMergedOptionsStore>();
            var key = DefaultTokenAcquirerFactoryImplementation.GetKey(options.Authority, options.ClientId, null);
            var mergedOptions = mergedOptionsStore.Get(key);

            Assert.Equal(options.ClientId, mergedOptions.ClientId);
            Assert.Equal(options.EnablePiiLogging, mergedOptions.EnablePiiLogging);
            Assert.Equal(options.AllowWebApiToBeAuthorizedByACL, mergedOptions.AllowWebApiToBeAuthorizedByACL);
            var namedOptions = provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(key);
            Assert.Equal(options.ClientId, namedOptions.ClientId);
            Assert.Equal(options.Authority, namedOptions.Authority);
            Assert.Same(options.ClientCredentials, namedOptions.ClientCredentials);
        }

        [Fact]
        public void GetTokenAcquirer_WithMicrosoftIdentityApplicationOptions_UsesAsIs()
        {
            // Arrange
            var taf = new CustomTAF();
            var provider = taf.Build();
            var factory = provider.GetRequiredService<ITokenAcquirerFactory>();

            var options = new MicrosoftIdentityApplicationOptions
            {
                ClientId = "test-client-id",
                Authority = "https://login.microsoftonline.com/test-tenant",
                EnablePiiLogging = true,
                AllowWebApiToBeAuthorizedByACL = true,
                Instance = "https://login.microsoftonline.com/",
                TenantId = "test-tenant-id",
                AzureRegion = "westus",
                SendX5C = true,
                Domain = "test-domain.com",
                SignUpSignInPolicyId = "B2C_1_signupsignin"
            };

            // Act
            var tokenAcquirer = factory.GetTokenAcquirer(options);

            // Assert
            Assert.NotNull(tokenAcquirer);
            
            // Verify the options were properly stored in the merged options
            var mergedOptionsStore = provider.GetRequiredService<IMergedOptionsStore>();
            var key = DefaultTokenAcquirerFactoryImplementation.GetKey(options.Authority, options.ClientId, options.AzureRegion);
            var mergedOptions = mergedOptionsStore.Get(key);

            Assert.Equal(options.ClientId, mergedOptions.ClientId);
            Assert.Equal(options.EnablePiiLogging, mergedOptions.EnablePiiLogging);
            Assert.Equal(options.Instance, mergedOptions.Instance);
            Assert.Equal(options.TenantId, mergedOptions.TenantId);
            Assert.Equal(options.AzureRegion, mergedOptions.AzureRegion);
            Assert.Equal(options.SendX5C, mergedOptions.SendX5C);
            Assert.Equal(options.Domain, mergedOptions.Domain);
            Assert.Equal(options.SignUpSignInPolicyId, mergedOptions.SignUpSignInPolicyId);
            Assert.Same(options, provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(key));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetTokenAcquirer_ProgrammaticSchemes_UseConsistentOptionsAcrossFactoryInstances(bool useStringOverload)
        {
            // Arrange
            var taf = new CustomTAF();
            var provider = taf.Build();
            var firstFactory = new DefaultTokenAcquirerFactoryImplementation(provider);
            var secondFactory = new DefaultTokenAcquirerFactoryImplementation(provider);
            var credentialsA = new[] { new CredentialDescription { SourceType = CredentialSource.ClientSecret, ClientSecret = "test-secret-a" } };
            var credentialsB = new[] { new CredentialDescription { SourceType = CredentialSource.ClientSecret, ClientSecret = "test-secret-b" } };
            var firstOptions = new MicrosoftIdentityApplicationOptions
            {
                Authority = "https://login.microsoftonline.com/test-tenant",
                ClientId = "test-client-id",
                AzureRegion = "westus",
                ClientCredentials = credentialsA,
            };
            var secondOptions = new MicrosoftIdentityApplicationOptions
            {
                Authority = firstOptions.Authority,
                ClientId = firstOptions.ClientId,
                AzureRegion = firstOptions.AzureRegion,
                ClientCredentials = credentialsB,
            };
            string key = DefaultTokenAcquirerFactoryImplementation.GetKey(
                firstOptions.Authority, firstOptions.ClientId, firstOptions.AzureRegion);

            // Act
            if (useStringOverload)
            {
                firstFactory.GetTokenAcquirer(firstOptions.Authority!, firstOptions.ClientId, credentialsA, firstOptions.AzureRegion);
                secondFactory.GetTokenAcquirer(secondOptions.Authority!, secondOptions.ClientId, credentialsB, secondOptions.AzureRegion);
            }
            else
            {
                firstFactory.GetTokenAcquirer(firstOptions);
                secondFactory.GetTokenAcquirer(secondOptions);
            }
            var namedOptions = provider.GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>().Get(key);
            var mergedOptions = provider.GetRequiredService<IMergedOptionsStore>().Get(key);

            // Assert
            Assert.Equal(firstOptions.ClientId, namedOptions.ClientId);
            Assert.Equal(firstOptions.Authority, namedOptions.Authority);
            Assert.Equal(firstOptions.AzureRegion, namedOptions.AzureRegion);
            Assert.Same(credentialsA, namedOptions.ClientCredentials);
            Assert.Same(namedOptions.ClientCredentials, mergedOptions.ClientCredentials);
            Assert.Equal(namedOptions.ClientId, mergedOptions.ClientId);
            Assert.Equal(namedOptions.Authority, mergedOptions.Authority);
            Assert.Same(credentialsA, firstOptions.ClientCredentials);
            Assert.Same(credentialsB, secondOptions.ClientCredentials);
        }

        private class CustomTAF : TokenAcquirerFactory
        {
            public CustomTAF()
            {
                this.Services.AddTokenAcquisition();
                this.Services.AddHttpClient();
                this.Services.AddSingleton<ITokenAcquirerFactory, DefaultTokenAcquirerFactoryImplementation>();
            }

            protected override string DefineConfiguration(IConfigurationBuilder builder)
            {
                return AppContext.BaseDirectory;
            }
        }
    }
}
