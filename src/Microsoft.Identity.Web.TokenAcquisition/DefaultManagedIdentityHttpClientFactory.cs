// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Adapts the base class <see cref="IHttpClientFactory"/> to MSAL's
    /// <see cref="IMsalHttpClientFactory"/> so managed-identity code
    /// can stay DI-friendly without static hooks.
    /// </summary>
    internal sealed class DefaultManagedIdentityHttpClientFactory
        : IManagedIdentityHttpClientFactory,
          IMsalHttpClientFactory
    {
        private readonly IHttpClientFactory _http;

        public DefaultManagedIdentityHttpClientFactory(IHttpClientFactory http) =>
            _http = http;

        public IMsalHttpClientFactory Create() => this;

        public HttpClient GetHttpClient() => _http.CreateClient();
    }
}
