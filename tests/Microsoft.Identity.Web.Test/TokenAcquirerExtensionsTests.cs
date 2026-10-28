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
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task GetFicTokenAsync_PreservesDictionaryComparerWithoutModifyingCaller(int dictionaryKind)
        {
            // Arrange
            IDictionary<string, object> callerExtraParameters = dictionaryKind switch
            {
                0 => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                1 => new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                2 => new SortedList<string, object>(StringComparer.OrdinalIgnoreCase),
                _ => throw new ArgumentOutOfRangeException(nameof(dictionaryKind))
            };
            callerExtraParameters["Configured"] = "value";
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
            IDictionary<string, object> copiedExtraParameters = capturedOptions!.ExtraParameters!;
            Assert.NotSame(callerExtraParameters, copiedExtraParameters);
            Assert.Equal("value", copiedExtraParameters["CONFIGURED"]);
            Assert.Equal("request-assertion", copiedExtraParameters[Constants.ClientAssertion]);
            Assert.Single(callerExtraParameters);
            Assert.False(callerExtraParameters.ContainsKey(Constants.ClientAssertion));
        }
    }
}
