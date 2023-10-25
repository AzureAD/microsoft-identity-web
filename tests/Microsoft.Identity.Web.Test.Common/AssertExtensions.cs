// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Xunit;

namespace Microsoft.Identity.Web.Test.Common
{
    public class Asserts
    {
        /// <summary>
        /// Verifies that a value is within a given range.
        /// </summary>
        /// <param name="actual">The actual value to be evaluated</param>
        /// <param name="expected">The expected middle of the range.</param>
        /// <param name="variance">The variance below and above the expected value.</param>
        public static void WithinVariance(TimeSpan actual, TimeSpan expected, TimeSpan variance)
        {
            Assert.InRange(actual, expected.Add(-variance), expected.Add(variance));
        }
    }
}
