// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AadIssuerValidatorTests
    {
        [Fact]
        public void NullArg()
        {
            // Arrange
            AadIssuerValidator validator = new AadIssuerValidator(TestConstants.s_aliases);
            var jwtSecurityToken = new JwtSecurityToken();
            var validationParams = new TokenValidationParameters();

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => validator.Validate(null, jwtSecurityToken, validationParams));
            Assert.Throws<ArgumentNullException>(() => validator.Validate(string.Empty, jwtSecurityToken, validationParams));
            Assert.Throws<ArgumentNullException>(() => validator.Validate(TestConstants.ClaimNameIss, null, validationParams));
            Assert.Throws<ArgumentNullException>(() => validator.Validate(TestConstants.ClaimNameIss, jwtSecurityToken, null));
        }

        [Fact]
        public void PassingValidation()
        {
            // Arrange
            AadIssuerValidator validator = new AadIssuerValidator(TestConstants.s_aliases);
            Claim issClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            Claim tidClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            // Act & Assert
            validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                new TokenValidationParameters() { ValidIssuers = new[] { TestConstants.AadIssuer } });
        }


        [Fact]
        public void TokenValidationParameters_ValidIssuer()
        {
            // Arrange
            AadIssuerValidator validator = new AadIssuerValidator(TestConstants.s_aliases);
            Claim issClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);
            Claim tidClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            // Act & Assert
            validator.Validate(TestConstants.AadIssuer, jwtSecurityToken,
                new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });
        }

        [Fact]
        public void ValidationSucceeds_NoTidClaimInJwt_TidCreatedFromIssuerInstead()
        {
            // Arrange
            AadIssuerValidator validator = new AadIssuerValidator(TestConstants.s_aliases);
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);

            JwtSecurityToken noTidJwt = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim });

            // Act & Assert
            validator.Validate(TestConstants.AadIssuer, noTidJwt,
                new TokenValidationParameters() { ValidIssuer = TestConstants.AadIssuer });
        }

        [Fact]
        public void ValidationFails_BadTidClaimInJwt()
        {
            // Arrange
            AadIssuerValidator validator = new AadIssuerValidator(TestConstants.s_aliases);
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            Claim tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.B2CTenantAsGuid);

            JwtSecurityToken noTidJwt = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            // Act & Assert
            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    TestConstants.AadIssuer,
                    noTidJwt,
                    new TokenValidationParameters() { ValidIssuers = new[] { TestConstants.AadIssuer } }));
        }

        [Fact]
        public void MultipleIssuers_NoneMatch()
        {
            // Arrange
            AadIssuerValidator validator = new AadIssuerValidator(TestConstants.s_aliases);
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            Claim tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);

            JwtSecurityToken noTidJwt = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            // Act & Assert
            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    TestConstants.AadIssuer,
                    noTidJwt,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[] {
                        "https://host1/{tenantid}/v2.0",
                        "https://host2/{tenantid}/v2.0"
                        }
                    }));
        }


        [Fact] // Regression test for https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/issues/68
        public void ValidationFails_BadIssuerClaimInJwt()
        {
            // Arrange
            string iss = $"https://badissuer/{TestConstants.TenantIdAsGuid}/v2.0";
            AadIssuerValidator validator = new AadIssuerValidator(TestConstants.s_aliases);
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.AadIssuer);
            Claim tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.TenantIdAsGuid);

            JwtSecurityToken noTidJwt = new JwtSecurityToken(issuer: TestConstants.AadIssuer, claims: new[] { issClaim, tidClaim });

            // Act & Assert
            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    iss,
                    noTidJwt,
                    new TokenValidationParameters() { ValidIssuers = new[] { TestConstants.AadIssuer } }));
        }

        [Fact]
        public void Validate_FromB2CAuthority_WithNoTidClaim_ValidateSuccessfully()
        {
            //Arrange
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CIssuer);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSuSiUserFlow);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CIssuer, claims: new[] { issClaim, tfpClaim });

            //Act
            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CAuthorityWithV2);

            //Assert
            validator.Validate(
                TestConstants.B2CIssuer,
                jwtSecurityToken,
                new TokenValidationParameters()
                {
                    ValidIssuers = new[] { TestConstants.B2CIssuer }
                });
        }

        [Fact]
        public void Validate_FromB2CAuthority_WithTidClaim_ValidateSuccessfully()
        {
            //Arrange
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CIssuer);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSuSiUserFlow);
            Claim tidClaim = new Claim(TestConstants.ClaimNameTid, TestConstants.B2CTenantAsGuid);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CIssuer, claims: new[] { issClaim, tfpClaim, tidClaim });

            //Act
            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CAuthorityWithV2);

            //Assert
            validator.Validate(
                TestConstants.B2CIssuer,
                jwtSecurityToken,
                new TokenValidationParameters()
                {
                    ValidIssuers = new[] { TestConstants.B2CIssuer }
                });
        }

        [Fact]
        public void Validate_FromB2CAuthority_InvalidIssuer_Fails()
        {
            //Arrange
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CIssuer2);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSuSiUserFlow);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CIssuer2, claims: new[] { issClaim, tfpClaim });

            //Act
            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CAuthorityWithV2);

            //Assert
            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    TestConstants.B2CIssuer2,
                    jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[] { TestConstants.B2CIssuer }
                    })
                );
        }

        [Fact]
        public void Validate_FromB2CAuthority_InvalidIssuerTid_Fails()
        {
            //Arrange
            string issuerWithInvalidTid = TestConstants.B2CInstance + "/" + TestConstants.TenantIdAsGuid + "/v2.0";
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, issuerWithInvalidTid);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSuSiUserFlow);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: issuerWithInvalidTid, claims: new[] { issClaim, tfpClaim });

            //Act
            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CAuthorityWithV2);

            //Assert
            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                validator.Validate(
                    issuerWithInvalidTid,
                    jwtSecurityToken,
                    new TokenValidationParameters()
                    {
                        ValidIssuers = new[] { TestConstants.B2CIssuer }
                    })
                );
        }

        [Fact]
        public void Validate_FromCustomB2CAuthority_ValidateSuccessfully()
        {
            //Arrange
            Claim issClaim = new Claim(TestConstants.ClaimNameIss, TestConstants.B2CCustomDomainIssuer);
            Claim tfpClaim = new Claim(TestConstants.ClaimNameTfp, TestConstants.B2CSuSiUserFlow);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(issuer: TestConstants.B2CCustomDomainIssuer, claims: new[] { issClaim, tfpClaim });

            //Act
            AadIssuerValidator validator = AadIssuerValidator.GetIssuerValidator(TestConstants.B2CCustomDomainAuthorityWithV2);

            //Assert
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