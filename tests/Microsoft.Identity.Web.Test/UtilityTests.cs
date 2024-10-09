// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Web.TokenCacheProviders;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class UtilityTests
    {
        [Fact(Skip = "Timing depends on resources of build VM, making the test have little value in a CI job")]
        public async Task Measurements_TestsAsync()
        {
            // measure task with no result
            var taskDuration = TimeSpan.FromMilliseconds(100);
            var epsilon = TimeSpan.FromMilliseconds(30).Ticks;
            var measure = await Task.Delay(taskDuration).MeasureAsync();

            Assert.True(Math.Abs(measure.Ticks - taskDuration.Ticks) < epsilon, $"{(measure.Ticks - taskDuration.Ticks) / TimeSpan.TicksPerMillisecond}ms exceeds epsilon of 30ms");
            Assert.Equal(measure.Ticks * 1000.0, measure.MilliSeconds);

            // measure task with a result
            var test = "test";
            var measureResult = await DelayAndReturnAsync(test, taskDuration).MeasureAsync();

            Assert.True(Math.Abs(measureResult.Ticks - taskDuration.Ticks) < epsilon);
            Assert.Same(test, measureResult.Result);

            // verify that an exception is thrown
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Task.Run(() => throw new InvalidOperationException()).MeasureAsync());
        }

        private async Task<string> DelayAndReturnAsync(string test, TimeSpan taskDuration)
        {
            await Task.Delay(taskDuration);
            return test;
        }
    }
}
