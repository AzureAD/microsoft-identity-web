// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class MicrosoftAuthenticationOptionsMerger : IPostConfigureOptions<MicrosoftAuthenticationOptions>
    {
        public MicrosoftAuthenticationOptionsMerger(IMergedOptionsStore mergedOptions)
        {
            _mergedOptionsMonitor = mergedOptions;
        }

        private readonly IMergedOptionsStore _mergedOptionsMonitor;

        public void PostConfigure(string name, MicrosoftAuthenticationOptions options)
        {
            MergedOptions.UpdateMergedOptionsFromMicrosoftAuthenticationOptions(options, _mergedOptionsMonitor.Get(name));
        }
    }
}
