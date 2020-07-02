// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Identity.Web.InstanceDiscovery
{
    /// <summary>
    /// Model child class to hold alias information parsed from the Azure AD issuer endpoint.
    /// </summary>
    internal class Metadata
    {
        /// <summary>
        /// Preferred alias.
        /// </summary>
        [JsonPropertyName(Constants.PreferredNetwork)]
        public string? PreferredNetwork { get; set; }

        /// <summary>
        /// Preferred alias to cache tokens emitted by one of the aliases (to avoid
        /// SSO islands).
        /// </summary>
        [JsonPropertyName(Constants.PreferredCache)]
        public string? PreferredCache { get; set; }

        /// <summary>
        /// Aliases of issuer URLs which are equivalent.
        /// </summary>
        [JsonPropertyName(Constants.Aliases)]
        public List<string> Aliases { get; set; } = new List<string>();
    }
}
