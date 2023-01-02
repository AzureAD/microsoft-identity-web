// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal sealed class ConfidentialClientApplicationOptionsMerger : IPostConfigureOptions<ConfidentialClientApplicationOptions>
    {
        public ConfidentialClientApplicationOptionsMerger(IMergedOptionsStore mergedOptions)
        {
            _mergedOptionsMonitor = mergedOptions;
        }

        private readonly IMergedOptionsStore _mergedOptionsMonitor;

        public void PostConfigure(
#if NET7_0_OR_GREATER
            string? name,
#else
            string name,
#endif
            ConfidentialClientApplicationOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(options, _mergedOptionsMonitor.Get(name ?? string.Empty));
        }
    }
}
