// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web
{
    internal class JwtBearerOptionsMerger : IPostConfigureOptions<JwtBearerOptions>
    {
        public JwtBearerOptionsMerger(IMergedOptionsStore mergedOptions)
        {
            _mergedOptionsMonitor = mergedOptions;
        }

        private readonly IMergedOptionsStore _mergedOptionsMonitor;

        public void PostConfigure(
#if NET6_0_OR_GREATER
            string? name,
#else
            string name,
#endif            
            JwtBearerOptions options)
        {
            MergedOptions mergedOptions = _mergedOptionsMonitor.Get(name ?? string.Empty);

            // Take into account extra query parameters
            UpdateOptionsMetadata(options, mergedOptions);
            MergedOptions.UpdateMergedOptionsFromJwtBearerOptions(options, mergedOptions);
        }

        private static void UpdateOptionsMetadata(JwtBearerOptions options, MergedOptions mergedOptions)
        {
            options.MetadataAddress ??= options.Authority + "/.well-known/openid-configuration";

            if (mergedOptions.ExtraQueryParameters != null)
            {
                options.MetadataAddress += options.MetadataAddress.Contains('?', StringComparison.OrdinalIgnoreCase) ? "&" : "?";
                options.MetadataAddress += string.Join("&", mergedOptions.ExtraQueryParameters.Select(p => $"{p.Key}={p.Value}"));
            }
        }
    }
}
