// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web.Test.Common;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class MicrosoftIdentityOptionsTests
    {
        [Fact]
        public void IsB2C_NotNullOrEmptyUserFlow_ReturnsTrue()
        {
            var options = new MicrosoftIdentityOptions()
            {
                SignUpSignInPolicyId = TestConstants.B2CSignUpSignInUserFlow
            };

            Assert.True(options.IsB2C);
        }

        [Fact]
        public void IsB2C_NullOrEmptyUserFlow_ReturnsFalse()
        {
            var options = new MicrosoftIdentityOptions();

            Assert.False(options.IsB2C);

            options.SignUpSignInPolicyId = string.Empty;

            Assert.False(options.IsB2C);
        }
    }
}
