// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Test
{
    internal sealed class TestManagedIdentityHttpFactory : IManagedIdentityTestHttpClientFactory
    {
        private readonly IMsalHttpClientFactory _msalHttpClientFactory;
        public TestManagedIdentityHttpFactory(IMsalHttpClientFactory msalHttpClientFactory)
            => _msalHttpClientFactory = msalHttpClientFactory;
        public IMsalHttpClientFactory Create() => _msalHttpClientFactory;
    }
}
