// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Post-configurator that populates MergedOptions from MicrosoftIdentityApplicationOptions for AOT scenarios.
    /// This enables TokenAcquisition to work unchanged by bridging the configuration models.
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
            // Get or create the MergedOptions for this scheme
            MergedOptions mergedOptions = _mergedOptionsStore.Get(name ?? string.Empty);

            // Populate MergedOptions from MicrosoftIdentityApplicationOptions
            // This reuses the existing UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions method
            MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(options, mergedOptions);
        }
    }
}

#endif
