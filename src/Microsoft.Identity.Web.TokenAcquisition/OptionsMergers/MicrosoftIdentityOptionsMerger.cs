// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    internal sealed class MicrosoftIdentityOptionsMerger : IPostConfigureOptions<MicrosoftIdentityOptions>
    {
        public MicrosoftIdentityOptionsMerger(IOptionsMonitor<MergedOptions> mergedOptions)
        {
            _mergedOptionsMonitor = mergedOptions;
        }

        private readonly IOptionsMonitor<MergedOptions> _mergedOptionsMonitor;

        public void PostConfigure(string name, MicrosoftIdentityOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(options, _mergedOptionsMonitor.Get(name));
        }
    }
}
