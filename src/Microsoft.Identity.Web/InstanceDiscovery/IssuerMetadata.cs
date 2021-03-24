// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT-License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Identity.Web.InstanceDiscovery
{
    /// <summary>
    /// Model class to hold information parsed from the Azure AD issuer endpoint.
    /// </summary>
    internal class IssuerMetadata
    {
        /// <summary>
        /// Tenant discovery endpoint.
        /// </summary>
        [JsonPropertyName(Constants.TenantDiscoveryEndpoint)]
        public string? TenantDiscoveryEndpoint { get; set; }

        /// <summary>
        /// API Version.
        /// </summary>
        [JsonPropertyName(Constants.ApiVersion)]
        public string? ApiVersion { get; set; }

        /// <summary>
        /// List of metadata associated with the endpoint.
        /// </summary>
        [JsonPropertyName(Constants.Metadata)]
        public List<Metadata> Metadata { get; set; } = new List<Metadata>();
    }
}
