// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Identity.Client;

namespace IntegrationTest.ClientBuilder
{
    /// <summary>
    /// MSAL HTTP Client factory for using custom HTTP client with MSAL.
    /// </summary>
    internal class MsalHttpClientFactory : IMsalHttpClientFactory
    {
        /// <summary>
        /// Name to use <see cref="IHttpClientFactory"/> registration.
        /// </summary>
        public const string HttpClientFactoryName = nameof(MsalHttpClientFactory);

        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsalHttpClientFactory"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        public MsalHttpClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Gets HTTP client.
        /// </summary>
        /// <returns>HTTP client instance.</returns>
        public HttpClient GetHttpClient()
        {
            return _httpClientFactory.CreateClient(HttpClientFactoryName);
        }
    }
}
