// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal class ConfidentialClientApplicationOptionsMerger : IPostConfigureOptions<ConfidentialClientApplicationOptions>
    {
        public ConfidentialClientApplicationOptionsMerger(IOptionsMonitor<MergedOptions> mergedOptions)
        {
            _mergedOptionsMonitor = mergedOptions;
        }

        private readonly IOptionsMonitor<MergedOptions> _mergedOptionsMonitor;

        public void PostConfigure(string name, ConfidentialClientApplicationOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromConfidentialClientApplicationOptions(options, _mergedOptionsMonitor.Get(name));
        }
    }
}
