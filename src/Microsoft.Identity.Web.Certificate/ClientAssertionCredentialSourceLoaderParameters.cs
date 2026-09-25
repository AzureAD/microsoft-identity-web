// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Loader parameters for client assertion warm-up.
    /// </summary>
    public sealed class ClientAssertionCredentialSourceLoaderParameters :
        CredentialSourceLoaderParameters, IClientAssertionEnrichmentOptions
    {
        /// <summary>
        /// Initializes client assertion loader parameters.
        /// </summary>
        /// <param name="clientId">Client ID of the application presenting the assertion.</param>
        /// <param name="authority">Authority to which the credential will be presented.</param>
        /// <param name="otelTagsEnricher">Optional warm-up telemetry enricher.</param>
        public ClientAssertionCredentialSourceLoaderParameters(
            string clientId,
            string authority,
            Action<ExecutionResult, IList<KeyValuePair<string, object>>>? otelTagsEnricher = null)
            : base(clientId, authority)
        {
            OtelTagsEnricher = otelTagsEnricher;
        }

        /// <inheritdoc/>
        public Action<ExecutionResult, IList<KeyValuePair<string, object>>>? OtelTagsEnricher { get; }
    }
}
