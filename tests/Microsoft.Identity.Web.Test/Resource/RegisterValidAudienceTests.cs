// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    public class RegisterValidAudienceTests
    {
        private const string Version = "ver";
        private const string Audience = "aud";
        private const string V1 = "1.0";
        private const string V2 = "2.0";
        private const string V3 = "3.0";

        [Theory]
        [InlineData(false, 1, V1)]
        [InlineData(false, 1, V2)]
        [InlineData(false, 4, V3)]
        [InlineData(false, 2, V1)]
        [InlineData(false, 3, V1)]
        [InlineData(false, 3, V2)]
        [InlineData(true, 1, V1)]
        [InlineData(true, 1, V2)]
        [InlineData(true, 2, V1)]
        [InlineData(true, 3, V2)]
        public void ValidateTokenAudience(
            bool isB2C,
            int testNumber,
            string tokenVersion)
        {
            MicrosoftIdentityOptions options = new MicrosoftIdentityOptions
            {
                ClientId = TestConstants.ClientId,
            };

            if (isB2C)
            {
                options.SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow;
            }

            string expectedAudience = string.Empty;
            if (tokenVersion == V2 || isB2C)
            {
                expectedAudience = options.ClientId;
            }
            else
            {
                expectedAudience = $"api://{options.ClientId}";
            }

            IEnumerable<Claim> claims = new Claim[]
            {
                  new Claim(Version, tokenVersion),
                  new Claim(Audience, expectedAudience),
            };

            JwtSecurityToken token = new JwtSecurityToken(null, null, claims);
            var validationParams = new TokenValidationParameters();
            RegisterValidAudience registerValidAudience = new RegisterValidAudience();
            registerValidAudience.RegisterAudienceValidation(validationParams, options);
            IEnumerable<string> validAudiences;
            validAudiences = new List<string> { expectedAudience };

            switch (testNumber)
            {
                case 1:
                    Assert.True(registerValidAudience.ValidateAudience(
                        validAudiences,
                        token,
                        validationParams));
                    Assert.Equal(expectedAudience, token.Audiences.FirstOrDefault());
                    Assert.Single(token.Audiences);
                    break;
                case 2:
                    validationParams.ValidAudience = expectedAudience;
                    Assert.True(registerValidAudience.ValidateAudience(
                        validAudiences,
                        token,
                        validationParams));
                    Assert.Equal(expectedAudience, token.Audiences.FirstOrDefault());
                    Assert.Single(token.Audiences);
                    break;
                case 3:
                    validationParams.ValidAudiences = new List<string>
                    {
                         $"api://{options.ClientId}",
                         options.ClientId,
                    };
                    Assert.True(registerValidAudience.ValidateAudience(
                        validAudiences,
                        token,
                        validationParams));
                    Assert.Equal(expectedAudience, token.Audiences.FirstOrDefault());
                    Assert.Single(token.Audiences);
                    break;
                case 4:
                    Assert.Throws<SecurityTokenValidationException>(() => registerValidAudience.ValidateAudience(
                    validAudiences,
                    token,
                    validationParams));
                    break;
            }
        }
    }
}
