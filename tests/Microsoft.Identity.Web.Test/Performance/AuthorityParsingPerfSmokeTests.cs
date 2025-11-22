// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Identity.Web;
using Xunit;

namespace Microsoft.Identity.Web.Test.Performance
{
    /// <summary>
    /// Performance smoke tests for authority parsing to guard against O(N²) or other performance regressions.
    /// Issue #3610: Optional performance validation for authority parsing logic.
    /// </summary>
    public class AuthorityParsingPerfSmokeTests
    {
        private const int IterationCount = 10000;
        private const int ThresholdMilliseconds = 1000; // Relaxed threshold for CI environments

        [Fact]
        public void ParseAuthority_ExecutesUnderThreshold_AAD()
        {
            // Issue #3610: 10k AAD authority parsing iterations should complete under 1000ms
            // This guards against accidental O(N²) or other performance degradation
            
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < IterationCount; i++)
            {
                var mergedOptions = new MergedOptions
                {
                    Authority = "https://login.microsoftonline.com/common/v2.0"
                };
                MergedOptions.ParseAuthorityIfNecessary(mergedOptions);
            }

            stopwatch.Stop();

            // Assert
            Assert.True(
                stopwatch.ElapsedMilliseconds < ThresholdMilliseconds,
                $"Performance regression detected: {IterationCount} iterations took {stopwatch.ElapsedMilliseconds}ms (threshold: {ThresholdMilliseconds}ms)"
            );
        }

        [Fact]
        public void ParseAuthority_ExecutesUnderThreshold_B2C()
        {
            // Issue #3610: 10k B2C authority parsing iterations should complete under 1000ms
            
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < IterationCount; i++)
            {
                var mergedOptions = new MergedOptions
                {
                    Authority = "https://fabrikamb2c.b2clogin.com/fabrikamb2c.onmicrosoft.com/B2C_1_susi/v2.0"
                };
                MergedOptions.ParseAuthorityIfNecessary(mergedOptions);
            }

            stopwatch.Stop();

            // Assert
            Assert.True(
                stopwatch.ElapsedMilliseconds < ThresholdMilliseconds,
                $"Performance regression detected: {IterationCount} iterations took {stopwatch.ElapsedMilliseconds}ms (threshold: {ThresholdMilliseconds}ms)"
            );
        }

        [Fact]
        public void ParseAuthority_ExecutesUnderThreshold_CIAM()
        {
            // Issue #3610: 10k CIAM authority parsing iterations should complete under 1000ms
            
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < IterationCount; i++)
            {
                var mergedOptions = new MergedOptions
                {
                    Authority = "https://contoso.ciamlogin.com/contoso.onmicrosoft.com",
                    PreserveAuthority = true
                };
                MergedOptions.ParseAuthorityIfNecessary(mergedOptions);
            }

            stopwatch.Stop();

            // Assert
            Assert.True(
                stopwatch.ElapsedMilliseconds < ThresholdMilliseconds,
                $"Performance regression detected: {IterationCount} iterations took {stopwatch.ElapsedMilliseconds}ms (threshold: {ThresholdMilliseconds}ms)"
            );
        }

        [Fact]
        public void PrepareAuthorityInstanceForMsal_ExecutesUnderThreshold()
        {
            // Issue #3610: 10k PrepareAuthorityInstanceForMsal iterations should complete under 1000ms
            
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < IterationCount; i++)
            {
                var mergedOptions = new MergedOptions
                {
                    Instance = "https://login.microsoftonline.com/",
                    TenantId = "common"
                };
                mergedOptions.PrepareAuthorityInstanceForMsal();
            }

            stopwatch.Stop();

            // Assert
            Assert.True(
                stopwatch.ElapsedMilliseconds < ThresholdMilliseconds,
                $"Performance regression detected: {IterationCount} iterations took {stopwatch.ElapsedMilliseconds}ms (threshold: {ThresholdMilliseconds}ms)"
            );
        }

        [Fact]
        public void PrepareAuthorityInstanceForMsal_B2C_RemovesTfp_ExecutesUnderThreshold()
        {
            // Issue #3610: 10k B2C PrepareAuthorityInstanceForMsal with /tfp/ should complete under 1000ms
            
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < IterationCount; i++)
            {
                var mergedOptions = new MergedOptions
                {
                    Instance = "https://fabrikamb2c.b2clogin.com/tfp/",
                    TenantId = "fabrikamb2c.onmicrosoft.com",
                    SignUpSignInPolicyId = "B2C_1_susi"
                };
                mergedOptions.PrepareAuthorityInstanceForMsal();
            }

            stopwatch.Stop();

            // Assert
            Assert.True(
                stopwatch.ElapsedMilliseconds < ThresholdMilliseconds,
                $"Performance regression detected: {IterationCount} iterations took {stopwatch.ElapsedMilliseconds}ms (threshold: {ThresholdMilliseconds}ms)"
            );
        }
    }
}
