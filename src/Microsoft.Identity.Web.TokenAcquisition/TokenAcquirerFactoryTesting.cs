// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.TestOnly
{
    /// <summary>
    /// Class that should only be used in tests, not product code, used
    /// to reset the default instance of the token acquirer factory.
    /// </summary>
    public static class TokenAcquirerFactoryTesting
    {
        /// <summary>
        /// Resets the default instance of the token acquirer factory.
        /// Use in tests, but not in production code.
        /// </summary>
        public static void ResetTokenAcquirerFactoryInTest()
        {
            TokenAcquirerFactory.ResetDefaultInstance();
        }

        /// <summary>
        /// Injects a custom <see cref="IMsalHttpClientFactory"/> so that all
        /// Managed-Identity calls made during the current test use the supplied
        /// (mocked) <see cref="HttpClient"/>.
        /// </summary>
        public static void UseTestHttpClientFactory(IMsalHttpClientFactory factory)
            => ManagedIdentityTestHooks.HttpClientFactoryOverride = factory;
    }
}
