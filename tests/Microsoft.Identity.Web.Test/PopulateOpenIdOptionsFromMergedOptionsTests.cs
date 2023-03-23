// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class PopulateOpenIdOptionsFromMergedOptionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PopulateOpenIdOptionsFromMergedOptions_WithValidOptions_PopulatesAllProperties(bool withCredentialDescription)
        {         
            // Arrange
            var options = new OpenIdConnectOptions();
            var mergedOptions = new MergedOptions
            {
                Authority = TestConstants.AuthorityCommonTenant,
                ClientId = TestConstants.ClientId,
                GetClaimsFromUserInfoEndpoint = true,
            };

            if (withCredentialDescription)
            {
                CredentialDescription credentialDescription = new()
                {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = TestConstants.ClientSecret
                };

                mergedOptions.ClientCredentials = new CredentialDescription[] { credentialDescription };
            }
            else
            {
                mergedOptions.ClientSecret = TestConstants.ClientSecret;
            }


            // Act
            MicrosoftIdentityWebAppAuthenticationBuilderExtensions.PopulateOpenIdOptionsFromMergedOptions(options, mergedOptions);

            // Assert
            Assert.Equal(options.Authority, mergedOptions.Authority);
            Assert.Equal(options.ClientId, mergedOptions.ClientId);
            Assert.Equal(options.GetClaimsFromUserInfoEndpoint, mergedOptions.GetClaimsFromUserInfoEndpoint);
            if (withCredentialDescription)
            {
                Assert.Equal(options.ClientSecret, mergedOptions.ClientCredentials?.FirstOrDefault(c => c.CredentialType == CredentialType.Secret)?.ClientSecret);
            }
            else
            {
                Assert.Equal(options.ClientSecret, mergedOptions.ClientSecret);
            }
        }
    }
}
