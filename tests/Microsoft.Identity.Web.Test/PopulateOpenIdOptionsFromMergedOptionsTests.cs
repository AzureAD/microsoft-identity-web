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
            var microsoftIdentityOptions = new MicrosoftIdentityOptions
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

                microsoftIdentityOptions.ClientCredentials = [credentialDescription];
            }
            else
            {
                microsoftIdentityOptions.ClientSecret = TestConstants.ClientSecret;
            }

            // Act
            MicrosoftIdentityWebAppAuthenticationBuilderExtensions.PopulateOpenIdOptionsFromMicrosoftIdentityOptions(options, microsoftIdentityOptions);

            // Assert
            Assert.Equal(options.Authority, microsoftIdentityOptions.Authority);
            Assert.Equal(options.ClientId, microsoftIdentityOptions.ClientId);
            Assert.Equal(options.GetClaimsFromUserInfoEndpoint, microsoftIdentityOptions.GetClaimsFromUserInfoEndpoint);
            if (withCredentialDescription)
            {
                Assert.Equal(options.ClientSecret, microsoftIdentityOptions.ClientCredentials?.FirstOrDefault(c => c.CredentialType == CredentialType.Secret)?.ClientSecret);
            }
            else
            {
                Assert.Equal(options.ClientSecret, microsoftIdentityOptions.ClientSecret);
            }
        }
    }
}
