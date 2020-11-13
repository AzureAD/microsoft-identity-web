// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Perf.Client
{
    public class Metrics
    {
        public int TotalRequests { get; set; }
        public double TotalRequestTimeInMilliseconds { get; set; }
        public double AverageRequestTimeInMilliseconds => TotalRequests > 0 ? TotalRequestTimeInMilliseconds / TotalRequests : 0;
        public int TotalAcquireTokenFailures { get; set; }
        public int TotalExceptions { get; set; }
        public int TotalTokensReturnedFromCache { get; set; }
        public double TotalMsalLookupTimeInMilliseconds { get; set; }
        public double AverageMsalLookupTimeInMilliseconds => TotalTokensReturnedFromCache > 0 ? TotalMsalLookupTimeInMilliseconds / TotalTokensReturnedFromCache : 0;
    }
}
