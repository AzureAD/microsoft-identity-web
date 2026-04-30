// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    /*
     * Used by Microsoft.Identity.Web
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
    internal sealed class MicrosoftIdentityOptionsMerger : IPostConfigureOptions<MicrosoftIdentityOptions>
    {
        public MicrosoftIdentityOptionsMerger(IMergedOptionsStore mergedOptions)
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
            MicrosoftIdentityOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityOptions(options, _mergedOptionsMonitor.Get(name ?? string.Empty));
        }
    }
}
