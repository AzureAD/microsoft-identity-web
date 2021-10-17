// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Identity.Web.InstanceDiscovery
{
    /// <summary>
    /// Model class to hold information parsed from the Azure AD issuer endpoint.
    /// </summary>
    internal class IssuerMetadata
    {
        /// <summary>
        /// Issuer associated with the OIDC endpoint.
        /// </summary>
        [JsonPropertyName("issuer")]
        public string? Issuer { get; set; }
    }
}
