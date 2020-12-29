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
        private JwtSecurityToken _token;
        private RegisterValidAudience _registerValidAudience;
        private TokenValidationParameters _validationParams;
        private IEnumerable<string> _validAudiences;
        private MicrosoftIdentityOptions _options;
        private string _expectedAudience;

        [Theory]
        [InlineData(false, V1)]
        [InlineData(false, V2)]
        [InlineData(true, V1)]
        public void ValidateAudience_FromToken(
            bool isB2C,
            string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion);
            AssertAudienceFromToken();
        }

        [Theory]
        [InlineData(false, V1)]
        [InlineData(false, V2)]
        [InlineData(true, V1)]
        public void ValidateAudience_ProvidedInValidAudience(
           bool isB2C,
           string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion);
            AssertAudienceProvidedInValidAudience();
        }

        [Theory]
        [InlineData(false, V1)]
        [InlineData(false, V2)]
        [InlineData(true, V1)]
        public void ValidateAudience_ProvidedInValidAudiences(
           bool isB2C,
           string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion);
            AssertAudienceProvidedInValidAudiences();
        }

        [Theory]
        [InlineData(false, V3)]
        public void InvalidAudience_AssertFails(
           bool isB2C,
           string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion);
            AssertFailureOnInvalidAudienceInToken();
        }

        private void InitializeTests(
            bool isB2C,
            string tokenVersion)
        {
            _options = new MicrosoftIdentityOptions
            {
                ClientId = TestConstants.ClientId,
            };

            if (isB2C)
            {
                // this may need to change if 'IsB2C' is changed to also allow for separate signup and signin user flows
                _options.SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow;
            }

            if (tokenVersion == V2 || isB2C)
            {
                _expectedAudience = _options.ClientId;
            }
            else
            {
                _expectedAudience = $"api://{_options.ClientId}";
            }

            IEnumerable<Claim> claims = new Claim[]
            {
                  new Claim(Version, tokenVersion),
                  new Claim(Audience, _expectedAudience),
            };

            _token = new JwtSecurityToken(null, null, claims);
            _validationParams = new TokenValidationParameters();
            _registerValidAudience = new RegisterValidAudience();
            _registerValidAudience.RegisterAudienceValidation(_validationParams, _options);
            _validAudiences = new List<string> { _expectedAudience };
        }

        private void AssertAudienceFromToken()
        {
            Assert.True(_registerValidAudience.ValidateAudience(
                       _validAudiences,
                       _token,
                       _validationParams));
            Assert.Equal(_expectedAudience, _token.Audiences.FirstOrDefault());
            Assert.Single(_token.Audiences);
        }

        private void AssertAudienceProvidedInValidAudience()
        {
            _validationParams.ValidAudience = _expectedAudience;
            Assert.True(_registerValidAudience.ValidateAudience(
                _validAudiences,
                _token,
                _validationParams));
            Assert.Equal(_expectedAudience, _token.Audiences.FirstOrDefault());
            Assert.Single(_token.Audiences);
        }

        private void AssertAudienceProvidedInValidAudiences()
        {
            _validationParams.ValidAudiences = new List<string>
                    {
                         $"api://{_options.ClientId}",
                         _options.ClientId,
                    };
            Assert.True(_registerValidAudience.ValidateAudience(
                _validAudiences,
                _token,
                _validationParams));
            Assert.Equal(_expectedAudience, _token.Audiences.FirstOrDefault());
            Assert.Single(_token.Audiences);
        }

        private void AssertFailureOnInvalidAudienceInToken()
        {
            Assert.Throws<SecurityTokenInvalidAudienceException>(() => _registerValidAudience.ValidateAudience(
                   _validAudiences,
                   _token,
                   _validationParams));
        }
    }
}
