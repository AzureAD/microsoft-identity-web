// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// **TEST-ONLY.** Allows unit tests to supply a custom <see cref="IMsalHttpClientFactory"/>.
    /// </summary>
    internal interface IManagedIdentityTestHttpClientFactory
    {
        IMsalHttpClientFactory Create();
    }
}
