// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ExceptionHandlingTest
    {
        [Fact]
        public void AuthorizeForScopesAttribute_FindMsalUiRequiredExceptionIfAny_Tests()
        {
            MsalUiRequiredException msalUiRequiredException = new MsalUiRequiredException("code", "message");

            MsalUiRequiredException result = AuthorizeForScopesAttribute.FindMsalUiRequiredExceptionIfAny(msalUiRequiredException);
            Assert.Equal(result, msalUiRequiredException);

            Exception ex = new Exception("message", msalUiRequiredException);
            result = AuthorizeForScopesAttribute.FindMsalUiRequiredExceptionIfAny(ex);
            Assert.Equal(result, msalUiRequiredException);

            Exception ex2 = new Exception("message", ex);
            result = AuthorizeForScopesAttribute.FindMsalUiRequiredExceptionIfAny(ex2);
            Assert.Equal(result, msalUiRequiredException);
        }
    }
}
