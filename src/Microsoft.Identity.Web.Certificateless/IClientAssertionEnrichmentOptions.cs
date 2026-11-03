// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Exposes operation-local telemetry enrichment to client assertion loaders.
    /// </summary>
    public interface IClientAssertionEnrichmentOptions
    {
        /// <summary>
        /// Gets the optional callback for the assertion acquisition.
        /// Loaders must not retain it on cached state.
        /// </summary>
        Action<ExecutionResult, IList<KeyValuePair<string, object>>>? OtelTagsEnricher { get; }
    }
}
