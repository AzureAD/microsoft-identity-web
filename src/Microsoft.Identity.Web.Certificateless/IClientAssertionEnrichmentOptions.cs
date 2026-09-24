// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Provides operation-local telemetry enrichment to a client assertion credential loader.
    /// Implement this on loader parameters to supply an enricher before MSAL requests an assertion.
    /// </summary>
    public interface IClientAssertionEnrichmentOptions
    {
        /// <summary>
        /// Gets the callback to forward to the assertion's token acquisition, or <c>null</c>
        /// if none is supplied. MSAL invokes it with the actual acquisition outcome.
        /// Loaders must not retain the callback on cached credentials or providers.
        /// </summary>
        Action<ExecutionResult, IList<KeyValuePair<string, object>>>? OtelTagsEnricher { get; }
    }
}
