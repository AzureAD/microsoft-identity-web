// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    internal class MicrosoftAuthenticationOptionsMerger : IPostConfigureOptions<MicrosoftAuthenticationOptions>
    {
        public MicrosoftAuthenticationOptionsMerger(IOptionsMonitor<MergedOptions> mergedOptions)
        {
            _mergedOptionsMonitor = mergedOptions;
        }

        private readonly IOptionsMonitor<MergedOptions> _mergedOptionsMonitor;

        public void PostConfigure(string name, MicrosoftAuthenticationOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromMicrosoftAuthenticationOptions(options, _mergedOptionsMonitor.Get(name));
        }
    }
}
