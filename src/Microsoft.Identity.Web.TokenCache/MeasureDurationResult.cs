// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Identity.Web.TokenCacheProviders
{
    internal readonly struct MeasureDurationResult
    {
        private const double Thousand = 1000.0;

        public MeasureDurationResult(long ticks)
        {
            Ticks = ticks;
        }

        public long Ticks { get; }

        public double MilliSeconds
        {
            get
            {
                return (double)Ticks / (double)Stopwatch.Frequency * Thousand;
            }
        }
    }

    internal readonly struct MeasureDurationResult<TResult>
    {
        public MeasureDurationResult(TResult result, long ticks)
        {
            Result = result;
            Ticks = ticks;
        }

        public TResult Result { get; }

        public long Ticks { get; }
    }
}
