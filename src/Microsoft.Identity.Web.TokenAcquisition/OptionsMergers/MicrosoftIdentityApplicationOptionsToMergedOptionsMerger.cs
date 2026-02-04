// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Post-configures MicrosoftIdentityApplicationOptions to merge into MergedOptions.
    /// This bridges the AOT-compatible path to the existing TokenAcquisition infrastructure.
    /// </summary>
    internal sealed class MicrosoftIdentityApplicationOptionsToMergedOptionsMerger : IPostConfigureOptions<MicrosoftIdentityApplicationOptions>
    {
        private readonly IMergedOptionsStore _mergedOptionsStore;

        public MicrosoftIdentityApplicationOptionsToMergedOptionsMerger(IMergedOptionsStore mergedOptionsStore)
        {
            _mergedOptionsStore = mergedOptionsStore;
        }

        public void PostConfigure(
#if NET7_0_OR_GREATER
            string? name,
#else
            string name,
#endif
            MicrosoftIdentityApplicationOptions options)
        {
            // Get the merged options for this scheme
            var mergedOptions = _mergedOptionsStore.Get(name ?? string.Empty);

            // Update merged options from application options
            // This uses the existing UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions method
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(options, mergedOptions);
        }
    }
}

#endif
