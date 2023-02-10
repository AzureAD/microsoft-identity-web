// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class AuthCodeRedemptionParametersTests
    {
        [Fact]
        public void AuthCodeRedemptionParametersTest()
        {
            string authCode = "1234cat56789";
            string authScheme = "Bearer";
            string clientInfo = "clientInfo";
            string codeVerifier = "1234verify56789";
            AuthCodeRedemptionParameters authCodeRedemptionParameters = new AuthCodeRedemptionParameters(
                TestConstants.s_userReadScope,
                authCode,
                authScheme,
                clientInfo,
                codeVerifier,
                TestConstants.B2CSignUpSignInUserFlow,
                TestConstants.TenantIdAsGuid);

            Assert.Equal(TestConstants.s_userReadScope, authCodeRedemptionParameters.Scopes);
            Assert.Equal(authCode, authCodeRedemptionParameters.AuthCode);
            Assert.Equal(authScheme, authCodeRedemptionParameters.AuthenticationScheme);
            Assert.Equal(clientInfo, authCodeRedemptionParameters.ClientInfo);
            Assert.Equal(codeVerifier, authCodeRedemptionParameters.CodeVerifier);
            Assert.Equal(TestConstants.B2CSignUpSignInUserFlow, authCodeRedemptionParameters.UserFlow);
            Assert.Equal(TestConstants.TenantIdAsGuid, authCodeRedemptionParameters.Tenant);
        }
    }
}
