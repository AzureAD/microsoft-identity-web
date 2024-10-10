// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.TokenCacheProviders
{
    /// <summary>
    /// Utility methods used by L1/L2 cache.
    /// </summary>
    internal static class Utility
    {
        internal static readonly Stopwatch s_watch = Stopwatch.StartNew();

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        internal static async Task<MeasureDurationResult> MeasureAsync(this Task task)
        {
            _ = Throws.IfNull(task);

            var startTicks = s_watch.Elapsed.Ticks;
            await task.ConfigureAwait(false);

            return new MeasureDurationResult(s_watch.Elapsed.Ticks - startTicks);
        }

        internal static async Task<MeasureDurationResult<TResult>> MeasureAsync<TResult>(this Task<TResult> task)
        {
            _ = Throws.IfNull(task);

            var startTicks = s_watch.Elapsed.Ticks;
            var taskResult = await task.ConfigureAwait(false);

            return new MeasureDurationResult<TResult>(taskResult, s_watch.Elapsed.Ticks - startTicks);
        }
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
    }
}
