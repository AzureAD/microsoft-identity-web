// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options passed-in to create the AadIssuerValidator object.
    /// </summary>
    public class AadIssuerValidatorOptions
    {
        /// <summary>
        /// Sets the name of the HttpClientFactory to use with the configuration manager.
        /// Needed when setting up a proxy.
        /// </summary>
        public string? HttpClientFactoryName { get; set; } = null!;
    }
}
