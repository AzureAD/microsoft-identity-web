// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /*
     * Used by Microsoft.Identity.Web
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
    internal sealed class MicrosoftIdentityApplicationOptionsMerger : IPostConfigureOptions<MicrosoftIdentityApplicationOptions>
    {
        public MicrosoftIdentityApplicationOptionsMerger(IMergedOptionsStore mergedOptions)
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
            MicrosoftIdentityApplicationOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(options, _mergedOptionsMonitor.Get(name ?? string.Empty));
        }
    }
}
