// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.TestOnly
{
    /// <summary>
    /// TEST-ONLY hook so unit tests can override the HttpClient factory used by
    /// ManagedIdentityClientAssertion.
    /// </summary>
    internal static class ManagedIdentityClientAssertionTestHook
    {
        /// <summary>
        /// Gets or sets the <see cref="IMsalHttpClientFactory"/> used by <c>ManagedIdentityClientAssertion</c> for unit testing purposes.
        /// </summary>
        internal static IMsalHttpClientFactory? HttpClientFactoryForTests { get; set; }
    }
}
