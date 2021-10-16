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
        /// Sets the name of the HttpClient to get from the IHttpClientFactory for use with the configuration manager.
        /// Needed when customizing the client such as configuring a proxy.
        /// </summary>
        public string? HttpClientName { get; set; }
    }
}
