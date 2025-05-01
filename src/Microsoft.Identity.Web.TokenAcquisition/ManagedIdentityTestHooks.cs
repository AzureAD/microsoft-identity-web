// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    ///  **TEST-ONLY hook** – unit-tests can inject a custom
    ///  <see cref="IMsalHttpClientFactory"/> so that MSAL’s Managed-Identity
    ///  pipeline uses a mocked <see cref="HttpClient"/> instead of making
    ///  real network calls.
    /// </summary>
    internal static class ManagedIdentityTestHooks
    {
        internal static IMsalHttpClientFactory? HttpClientFactoryOverride { get; set; }
    }
}
