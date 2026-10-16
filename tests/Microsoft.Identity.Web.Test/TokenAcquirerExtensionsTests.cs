// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TokenAcquirerExtensionsTests
    {
        [Fact]
        public async Task GetFicTokenAsync_WithUnknownDictionary_CopiesContentsWithoutModifyingCaller()
        {
            // Arrange
            var callerExtraParameters = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Configured"] = "value"
            };
            var callerOptions = new AcquireTokenOptions
            {
                ExtraParameters = callerExtraParameters
            };
            AcquireTokenOptions? capturedOptions = null;
            ITokenAcquirer tokenAcquirer = Substitute.For<ITokenAcquirer>();
            tokenAcquirer
                .GetTokenForAppAsync(
                    Arg.Any<string>(),
                    Arg.Any<AcquireTokenOptions>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    capturedOptions = callInfo.ArgAt<AcquireTokenOptions>(1);
                    return Task.FromResult<AcquireTokenResult>(null!);
                });

            // Act
            await tokenAcquirer.GetFicTokenAsync(callerOptions, "request-assertion");

            // Assert
            Dictionary<string, object> copiedExtraParameters =
                Assert.IsType<Dictionary<string, object>>(capturedOptions!.ExtraParameters);
            Assert.Equal("value", copiedExtraParameters["Configured"]);
            Assert.False(copiedExtraParameters.ContainsKey("CONFIGURED"));
            Assert.Equal("request-assertion", copiedExtraParameters[Constants.ClientAssertion]);
            Assert.Single(callerExtraParameters);
            Assert.False(callerExtraParameters.ContainsKey(Constants.ClientAssertion));
        }
    }
}
