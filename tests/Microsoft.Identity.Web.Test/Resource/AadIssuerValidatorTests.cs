// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    public class AadIssuerValidatorTests
    {
        private readonly MicrosoftIdentityIssuerValidatorFactory _issuerValidatorFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public AadIssuerValidatorTests()
        {
            _httpClientFactory = new HttpClientFactoryTest();
            _issuerValidatorFactory = new MicrosoftIdentityIssuerValidatorFactory(
                null,
                _httpClientFactory);
        }

        [Fact]
        public void GetIssuerValidator_NullOrEmptyAuthority_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(TestConstants.AadAuthority, () => _issuerValidatorFactory.GetAadIssuerValidator(string.Empty));

            Assert.Throws<ArgumentNullException>(TestConstants.AadAuthority, () => _issuerValidatorFactory.GetAadIssuerValidator(null));
        }

        [Fact]
        public void GetIssuerValidator_InvalidAuthority_ReturnsValidatorBasedOnFallbackAuthority()
        {
            var validator = _issuerValidatorFactory.GetAadIssuerValidator(TestConstants.InvalidAuthorityFormat);

            Assert.NotNull(validator);
        }

        [Fact]
        public void GetIssuerValidator_AuthorityInAliases_ReturnsValidator()
        {
            var authorityInAliases = TestConstants.AuthorityCommonTenantWithV2;

            var validator = _issuerValidatorFactory.GetAadIssuerValidator(authorityInAliases);

            Assert.NotNull(validator);
        }

        [Fact]
        public void GetIssuerValidator_B2cAuthorityNotInAliases_ReturnsValidator()
        {
            var authorityNotInAliases = TestConstants.B2CAuthorityWithV2;

            var validator = _issuerValidatorFactory.GetAadIssuerValidator(authorityNotInAliases);

            Assert.NotNull(validator);
        }

        [Fact]
        public void GetIssuerValidator_CachedAuthority_ReturnsCachedValidator()
        {
            var authorityNotInAliases = TestConstants.AuthorityWithTenantSpecifiedWithV2;

            var validator1 = _issuerValidatorFactory.GetAadIssuerValidator(authorityNotInAliases);
            var validator2 = _issuerValidatorFactory.GetAadIssuerValidator(authorityNotInAliases);

            Assert.Same(validator1, validator2);
        }

        [Fact]
        public void Validate_NullOrEmptyParameters_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var jwtSecurityToken = new JwtSecurityToken();
            var validationParams = new TokenValidationParameters();

            Assert.Throws<ArgumentNullException>(TestConstants.ActualIssuer, () => validator.Validate(null, jwtSecurityToken, validationParams));

            Assert.Throws<ArgumentNullException>(TestConstants.ActualIssuer, () => validator.Validate(string.Empty, jwtSecurityToken, validationParams));

            Assert.Throws<ArgumentNullException>(TestConstants.SecurityToken, () => validator.Validate(TestConstants.AadIssuer, null, validationParams));

            Assert.Throws<ArgumentNullException>(TestConstants.ValidationParameters, () => validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, null));
        }

        [Fact]
        public void Validate_NullOrEmptyTenantId_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var jwtSecurityToken = new JwtSecurityToken();
            var jsonWebToken = new JsonWebToken($"{{}}", $"{{}}");
            var securityToken = Substitute.For<SecurityToken>();
            var validationParameters = new TokenValidationParameters();

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() => validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, validationParameters));
            Assert.Equal(IDWebErrorMessage.TenantIdClaimNotPresentInToken, exception.Message);

            exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() => validator.Validate(TestConstants.AadIssuer, jsonWebToken, validationParameters));
            Assert.Equal(IDWebErrorMessage.TenantIdClaimNotPresentInToken, exception.Message);

            exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() => validator.Validate(TestConstants.AadIssuer, securityToken, validationParameters));
            Assert.Equal(IDWebErrorMessage.TenantIdClaimNotPresentInToken, exception.Message);
        }

        [Theory]
        [InlineData(TestConstants.ClaimNameTid)]
        [InlineData(ClaimConstants.TenantId)]
        public void Validate_IssuerMatchedInValidIssuer_ReturnsIssuer(string tidClaimType)
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var tidClaim = new Claim(tidClaimType, TestConstants.TenantIdAsGuid);

            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            var actualIssuer = validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);
        }

        [Theory]
        [InlineData(TestConstants.ClaimNameTid)]
        [InlineData(ClaimConstants.TenantId)]
        public void Validate_IssuerMatchedInValidIssuers_ReturnsIssuer(string tidClaimType)
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var tidClaim = new Claim(tidClaimType, TestConstants.TenantIdAsGuid);

            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            var actualIssuer = validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, new TokenValidationParameters() { ValidIssuers = new[] { TestConstants.AadIssuer } });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);
        }

        [Fact]
        public void Validate_TenantIdInIssuerNotInToken_ReturnsIssuer()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim });

            var actualIssuer = validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);
        }

        [Fact]
        public void Validate_TidClaimInToken_ReturnsIssuer()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });
            var jsonWebToken = new JsonWebToken($"{{}}", $"{{\"{TestConstants.ClaimNameIss}\":\"{TestConstants.AadIssuer}\",\"{TestConstants.ClaimNameTid}\":\"{TestConstants.TenantIdAsGuid}\"}}");

            var actualIssuer = validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);

            actualIssuer = validator.Validate(TestConstants.AadIssuer, jsonWebToken, new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);
        }

        [Fact]
        public void Validate_NotMatchedIssuer_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });
            var expectedErrorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    IDWebErrorMessage.IssuerDoesNotMatchValidIssuers,
                    TestConstants.AadIssuer);

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, new TokenValidationParameters() { ValidIssuer = TestConstants.B2CIssuer }));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void Validate_NotMatchedTenantIds_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.B2CTenantAsGuid);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });
            var expectedErrorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    IDWebErrorMessage.IssuerDoesNotMatchValidIssuers,
                    TestConstants.AadIssuer);

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer }));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void Validate_NotMatchedToMultipleIssuers_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });
            var expectedErrorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    IDWebErrorMessage.IssuerDoesNotMatchValidIssuers,
                    TestConstants.AadIssuer);

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    TestConstants.AadIssuer,
                    jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[]
                        {
                            "https://host1/{tenantid}/v2.0",
                            "https://host2/{tenantid}/v2.0",
                        },
                    }));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        // Regression test for https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/issues/68
        // Similar to Validate_NotMatchedToMultipleIssuers_ThrowsException but uses B2C values
        [Fact]
        public void Validate_InvalidIssuerToValidate_ThrowsException()
        {
            string invalidIssuerToValidate = $"https://badissuer/{TestConstants.TenantIdAsGuid}/v2.0";
            AadIssuerValidator validator = new AadIssuerValidator(TestConstants.s_aliases);
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            Claim tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });
            var expectedErrorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    IDWebErrorMessage.IssuerDoesNotMatchValidIssuers,
                    invalidIssuerToValidate);

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(invalidIssuerToValidate, jwtSecurityToken, new TokenValidationParameters() { ValidIssuers = new[] { TestConstants.AadIssuer } }));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        // Similar to Validate_TenantIdInIssuerNotInToken_ReturnsIssuer but uses
        // GetIssuerValidator instead of the constructor and B2C values
        [Fact]
        public void Validate_FromB2CAuthority_WithNoTidClaim_ValidateSuccessfully()
        {
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CIssuer);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSignUpSignInUserFlow);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CIssuer, claims: new[] { issClaim, tfpClaim });

            AadIssuerValidator validator = _issuerValidatorFactory.GetAadIssuerValidator(TestConstants.B2CAuthorityWithV2);

            validator.Validate(
                TestConstants.B2CIssuer,
                jwtSecurityToken,
                new TokenValidationParameters()
                {
                    ValidIssuers = new[] { TestConstants.B2CIssuer },
                });
        }

        // Similar to Validate_TidClaimInToken_ReturnsIssuer but uses
        // GetIssuerValidator instead of the constructor and B2C values
        [Fact]
        public void Validate_FromB2CAuthority_WithTidClaim_ValidateSuccessfully()
        {
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CIssuer);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSignUpSignInUserFlow);
            Claim tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.B2CTenantAsGuid);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CIssuer, claims: new[] { issClaim, tfpClaim, tidClaim });

            AadIssuerValidator validator = _issuerValidatorFactory.GetAadIssuerValidator(TestConstants.B2CAuthorityWithV2);

            validator.Validate(
                TestConstants.B2CIssuer,
                jwtSecurityToken,
                new TokenValidationParameters()
                {
                    ValidIssuers = new[] { TestConstants.B2CIssuer },
                });
        }

        // Similar to Validate_NotMatchedIssuer_ThrowsException and Validate_NotMatchedToMultipleIssuers_ThrowsException but uses
        // GetIssuerValidator instead of the constructor and B2C values
        [Fact]
        public void Validate_FromB2CAuthority_InvalidIssuer_Fails()
        {
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CIssuer2);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSignUpSignInUserFlow);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CIssuer2, claims: new[] { issClaim, tfpClaim });

            AadIssuerValidator validator = _issuerValidatorFactory.GetAadIssuerValidator(TestConstants.B2CAuthorityWithV2);

            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    TestConstants.B2CIssuer2,
                    jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[] { TestConstants.B2CIssuer },
                    }));
        }

        // Similar to Validate_NotMatchedTenantIds_ThrowsException but uses
        // GetIssuerValidator instead of the constructor and B2C values
        [Fact]
        public void Validate_FromB2CAuthority_InvalidIssuerTid_Fails()
        {
            string issuerWithInvalidTid = TestConstants.B2CInstance + "/" + TestConstants.TenantIdAsGuid + "/v2.0";
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, issuerWithInvalidTid);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSignUpSignInUserFlow);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: issuerWithInvalidTid, claims: new[] { issClaim, tfpClaim });

            AadIssuerValidator validator = _issuerValidatorFactory.GetAadIssuerValidator(TestConstants.B2CAuthorityWithV2);

            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    issuerWithInvalidTid,
                    jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[] { TestConstants.B2CIssuer },
                    }));
        }

        // Similar to Validate_IssuerMatchedInValidIssuers_ReturnsIssuer but uses
        // GetIssuerValidator instead of the constructor and B2C values
        [Fact]
        public void Validate_FromCustomB2CAuthority_ValidateSuccessfully()
        {
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CCustomDomainIssuer);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSignUpSignInUserFlow);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CCustomDomainIssuer, claims: new[] { issClaim, tfpClaim });

            AadIssuerValidator validator = _issuerValidatorFactory.GetAadIssuerValidator(TestConstants.B2CCustomDomainAuthorityWithV2);

            validator.Validate(
                TestConstants.B2CCustomDomainIssuer,
                jwtSecurityToken,
                new TokenValidationParameters()
                {
                    ValidIssuers = new[] { TestConstants.B2CCustomDomainIssuer },
                });
        }

        [Fact]
        public void Validate_FromB2CAuthority_WithTfpIssuer_ThrowsException()
        {
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CIssuerTfp);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CIssuerTfp, claims: new[] { issClaim });

            AadIssuerValidator validator = _issuerValidatorFactory.GetAadIssuerValidator(TestConstants.B2CAuthorityWithV2);

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    TestConstants.B2CIssuerTfp,
                    jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[] { TestConstants.B2CIssuerTfp },
                    }));
            Assert.Equal(IDWebErrorMessage.B2CTfpIssuerNotSupported, exception.Message);
        }
    }
}
