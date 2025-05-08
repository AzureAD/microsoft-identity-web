// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.TestOnly
{
    /// <summary>
    /// INTERNAL – intended solely for unit-tests.
    /// Allows tests to inject their own IMsalHttpClientFactory so that
    /// Managed-Identity flows use mocked HTTP Client Factory.
    /// </summary>
    internal static class TokenAcquirerTestHooks
    {
        /// <remarks>
        /// Tests call this *before* the first MI token request.
        /// Product code never sets this value.
        /// </remarks>
        internal static void UseTestHttpClientFactory(IMsalHttpClientFactory factory)
            => HttpClientFactoryOverride = factory;

        internal static IMsalHttpClientFactory? HttpClientFactoryOverride { get; private set; }
    }
}
