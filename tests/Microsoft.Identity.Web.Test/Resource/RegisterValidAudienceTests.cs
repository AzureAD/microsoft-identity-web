// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.JsonWebTokens;
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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private SecurityToken _token;
        private RegisterValidAudience _registerValidAudience;
        private TokenValidationParameters _validationParams;
        private IEnumerable<string> _validAudiences;
        private MicrosoftIdentityOptions _options;
        private string _expectedAudience;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Theory]
        [InlineData(false, V1)]
        [InlineData(false, V2)]
        [InlineData(true, V1)]
        public void ValidateAudience_FromToken(
            bool isB2C,
            string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion, claims => new JwtSecurityToken(null, null, claims));
            AssertAudienceFromToken();
        }

        [Theory]
        [InlineData(false, V1)]
        [InlineData(false, V2)]
        [InlineData(true, V1)]
        public void ValidateAudience_FromToken_JsonWeb(
            bool isB2C,
            string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion, claims => new TestJsonWebToken(claims));
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
            InitializeTests(isB2C, tokenVersion, claims => new JwtSecurityToken(null, null, claims));
            AssertAudienceProvidedInValidAudience();
        }

        [Theory]
        [InlineData(false, V1)]
        [InlineData(false, V2)]
        [InlineData(true, V1)]
        public void ValidateAudience_ProvidedInValidAudience_JsonWeb(
           bool isB2C,
           string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion, claims => new TestJsonWebToken(claims));
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
            InitializeTests(isB2C, tokenVersion, claims => new JwtSecurityToken(null, null, claims));
            AssertAudienceProvidedInValidAudiences();
        }

        [Theory]
        [InlineData(false, V1)]
        [InlineData(false, V2)]
        [InlineData(true, V1)]
        public void ValidateAudience_ProvidedInValidAudiences_JsonWeb(
           bool isB2C,
           string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion, claims => new TestJsonWebToken(claims));
            AssertAudienceProvidedInValidAudiences();
        }

        [Theory]
        [InlineData(false, V3)]
        public void InvalidAudience_AssertFails(
           bool isB2C,
           string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion, claims => new JwtSecurityToken(null, null, claims));
            AssertFailureOnInvalidAudienceInToken();
        }

        [Theory]
        [InlineData(false, V3)]
        public void InvalidAudience_AssertFails_JsonWeb(
           bool isB2C,
           string tokenVersion)
        {
            InitializeTests(isB2C, tokenVersion, claims => new TestJsonWebToken(claims));
            AssertFailureOnInvalidAudienceInToken();
        }

        private void InitializeTests(
            bool isB2C,
            string tokenVersion,
            Func<IEnumerable<Claim>, SecurityToken> tokenGenerator)
        {
            _options = new MicrosoftIdentityOptions
            {
                ClientId = TestConstants.ClientId,
            };

            if (isB2C)
            {
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

            _token = tokenGenerator(claims);
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
            Assert.Equal(_expectedAudience, Audiences.FirstOrDefault());
            Assert.Single(Audiences);
        }

        private void AssertAudienceProvidedInValidAudience()
        {
            _validationParams.ValidAudience = _expectedAudience;
            Assert.True(_registerValidAudience.ValidateAudience(
                _validAudiences,
                _token,
                _validationParams));
            Assert.Equal(_expectedAudience, Audiences.FirstOrDefault());
            Assert.Single(Audiences);
        }

        private void AssertAudienceProvidedInValidAudiences()
        {
            Assert.NotNull(_options);
            Assert.NotNull(_options.ClientId);
            _validationParams.ValidAudiences = new List<string>
                    {
                         $"api://{_options.ClientId}",
                         _options.ClientId,
                    };
            Assert.True(_registerValidAudience.ValidateAudience(
                _validAudiences,
                _token,
                _validationParams));
            Assert.Equal(_expectedAudience, Audiences.FirstOrDefault());
            Assert.Single(Audiences);
        }

        private IEnumerable<string> Audiences => _token switch
        {
            JwtSecurityToken s => s.Audiences,
            TestJsonWebToken w => w.Audiences,
            _ => throw new System.NotImplementedException(),
        };

        private void AssertFailureOnInvalidAudienceInToken()
        {
            Assert.Throws<SecurityTokenInvalidAudienceException>(() => _registerValidAudience.ValidateAudience(
                   _validAudiences,
                   _token,
                   _validationParams));
        }

        private class TestJsonWebToken : JsonWebToken
        {
            private const string TestJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            public TestJsonWebToken(IEnumerable<Claim> claims)
                : base(TestJwt)
            {
                Claims = claims;
            }

            public override IEnumerable<Claim> Claims { get; }

            public new IEnumerable<string> Audiences => Claims.Where(c => c.Type == Audience).Select(c => c.Value);
        }
    }
}
