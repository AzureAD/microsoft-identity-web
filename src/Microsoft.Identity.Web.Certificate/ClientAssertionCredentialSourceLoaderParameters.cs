// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Credential loader parameters that carry an OpenTelemetry tags enricher for assertion warm-up.
    /// Compatible with existing loaders accepting <see cref="CredentialSourceLoaderParameters"/>.
    /// </summary>
    public sealed class ClientAssertionCredentialSourceLoaderParameters :
        CredentialSourceLoaderParameters, IClientAssertionEnrichmentOptions
    {
        /// <summary>
        /// Initializes operation-local parameters for loading a client assertion credential.
        /// </summary>
        /// <param name="clientId">Client ID of the application presenting the assertion.</param>
        /// <param name="authority">Authority to which the credential will be presented.</param>
        /// <param name="otelTagsEnricher">Optional callback forwarded to the warm-up acquisition.</param>
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
