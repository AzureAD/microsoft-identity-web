// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test.Resource
{
    public class MicrosoftIdentityIssuerValidatorTests
    {
        private readonly MicrosoftIdentityIssuerValidatorFactory _issuerValidatorFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public MicrosoftIdentityIssuerValidatorTests()
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
    }
}
