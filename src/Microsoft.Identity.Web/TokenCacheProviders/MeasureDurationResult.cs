// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.TokenCacheProviders
{
    internal struct MeasureDurationResult
    {
        public MeasureDurationResult(long ticks)
        {
            Ticks = ticks;
        }

        public long Ticks { get; }
    }

    internal struct MeasureDurationResult<TResult>
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
