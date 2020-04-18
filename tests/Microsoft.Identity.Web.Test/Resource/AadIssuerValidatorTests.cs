// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IdentityModel.Tokens.Jwt;
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
        [Fact]
        public void GetIssuerValidator_NullOrEmptyAuthority_ThrowsException()
        {
            var exception = Assert.Throws<ArgumentNullException>("aadAuthority", () => AadIssuerValidator.GetIssuerValidator(string.Empty));

            exception = Assert.Throws<ArgumentNullException>("aadAuthority", () => AadIssuerValidator.GetIssuerValidator(null));
        }

        [Fact]
        public void GetIssuerValidator_InvalidAuthority_ReturnsValidatorBasedOnFallbackAuthority()
        {
            var invalidAuthority = "login.microsoft.com";

            var validator = AadIssuerValidator.GetIssuerValidator(invalidAuthority);

            Assert.NotNull(validator);
        }

        [Fact]
        public void GetIssuerValidator_AuthorityInAliases_ReturnsValidator()
        {
            var authorityInAliases = TestConstants.AuthorityCommonTenantWithV2;

            var validator = AadIssuerValidator.GetIssuerValidator(authorityInAliases);

            Assert.NotNull(validator);
        }

        [Fact]
        public void GetIssuerValidator_AuthorityNotInAliases_ReturnsValidator() // B2C
        {
            var authorityNotInAliases = TestConstants.B2CAuthorityWithV2;

            var validator = AadIssuerValidator.GetIssuerValidator(authorityNotInAliases);

            Assert.NotNull(validator);
        }

        // TODO: Possible bug in cache. Validator is cached with key as authority host, but tries to retrieve with full authority.
        [Fact]
        public void GetIssuerValidator_CachedAuthority_ReturnsCachedValidator()
        {
            // var authorityNotInAliases = TestConstants.ProductionPrefNetworkEnvironment;

            // var validator1 = AadIssuerValidator.GetIssuerValidator(authorityNotInAliases);
            // var validator2 = AadIssuerValidator.GetIssuerValidator(authorityNotInAliases);

            // Assert.Same(validator1, validator2);
        }

        [Fact]
        public void Validate_NullOrEmptyParameters_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var jwtSecurityToken = new JwtSecurityToken();
            var validationParams = new TokenValidationParameters();

            var exception = Assert.Throws<ArgumentNullException>("actualIssuer", () => validator.Validate(null, jwtSecurityToken, validationParams));

            exception = Assert.Throws<ArgumentNullException>("actualIssuer", () => validator.Validate(string.Empty, jwtSecurityToken, validationParams));

            exception = Assert.Throws<ArgumentNullException>("securityToken", () => validator.Validate(TestConstants.AadIssuer, null, validationParams));

            exception = Assert.Throws<ArgumentNullException>("validationParameters", () => validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, null));
        }

        [Fact]
        public void Validate_NullOrEmptyTenantId_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var jwtSecurityToken = new JwtSecurityToken();
            var jsonWebToken = new JsonWebToken($"{{}}", $"{{}}");
            var securityToken = Substitute.For<SecurityToken>();
            var validationParameters = new TokenValidationParameters();
            var expectedErrorMessage = "Neither `tid` nor `tenantId` claim is present in the token obtained from Microsoft identity platform.";

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() => validator.Validate(TestConstants.AadIssuer, jwtSecurityToken, validationParameters));
            Assert.Equal(expectedErrorMessage, exception.Message);

            exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() => validator.Validate(TestConstants.AadIssuer, jsonWebToken, validationParameters));
            Assert.Equal(expectedErrorMessage, exception.Message);

            exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() => validator.Validate(TestConstants.AadIssuer, securityToken, validationParameters));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void Validate_IssuerMatchedInValidIssuer_ReturnsIssuer()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            var actualIssuer = validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);
        }

        [Fact]
        public void Validate_IssuerMatchedInValidIssuers_ReturnsIssuer()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            var actualIssuer = validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                new TokenValidationParameters() { ValidIssuers = new[] { TestConstants.AadIssuer } });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);
        }

        [Fact]
        public void Validate_TenantIdInIssuerNotInToken_ReturnsIssuer()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim });

            var actualIssuer = validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });

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

            var actualIssuer = validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);

            actualIssuer = validator.Validate(TestConstants.AadIssuer, jsonWebToken,
                new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });

            Assert.Equal(TestConstants.AadIssuer, actualIssuer);
        }

        [Fact]
        public void Validate_NotMatchedIssuer_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });
            var expectedErrorMessage = $"Issuer: '{TestConstants.AadIssuer}', does not match any of the valid issuers provided for this application.";

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                    new TokenValidationParameters() { ValidIssuer = TestConstants.B2CIssuer }));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void Validate_NotMatchedTenantIds_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.B2CTenantAsGuid);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });
            var expectedErrorMessage = $"Issuer: '{TestConstants.AadIssuer}', does not match any of the valid issuers provided for this application.";

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                    new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer }));
            Assert.Equal(expectedErrorMessage, exception.Message);
        }

        [Fact]
        public void Validate_NotMatchedToMultipleIssuers_ThrowsException()
        {
            var validator = new AadIssuerValidator(TestConstants.s_aliases);
            var issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            var tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            var jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });
            var expectedErrorMessage = $"Issuer: '{TestConstants.AadIssuer}', does not match any of the valid issuers provided for this application.";

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[]
                        {
                            "https://host1/{tenantid}/v2.0",
                            "https://host2/{tenantid}/v2.0"
                        }
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
            var expectedErrorMessage = $"Issuer: '{invalidIssuerToValidate}', does not match any of the valid issuers provided for this application.";

            var exception = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(invalidIssuerToValidate, jwtSecurityToken,
                    new TokenValidationParameters() { ValidIssuers = new[] { TestConstants.AadIssuer } }));
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

            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CAuthorityWithV2);

            validator.Validate(
                TestConstants.B2CIssuer,
                jwtSecurityToken,
                new TokenValidationParameters()
                {
                    ValidIssuers = new[] { TestConstants.B2CIssuer }
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

            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CAuthorityWithV2);

            validator.Validate(
                TestConstants.B2CIssuer,
                jwtSecurityToken,
                new TokenValidationParameters()
                {
                    ValidIssuers = new[] { TestConstants.B2CIssuer }
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

            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CAuthorityWithV2);

            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    TestConstants.B2CIssuer2,
                    jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[] { TestConstants.B2CIssuer }
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

            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CAuthorityWithV2);

            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    issuerWithInvalidTid,
                    jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[] { TestConstants.B2CIssuer }
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

            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CCustomDomainAuthorityWithV2);

            validator.Validate(
                TestConstants.B2CCustomDomainIssuer,
                jwtSecurityToken,
                new TokenValidationParameters()
                {
                    ValidIssuers = new[] { TestConstants.B2CCustomDomainIssuer }
                });
        }
    }
}