// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal sealed class MsalAspNetCoreHttpClientFactory : IMsalHttpClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MsalAspNetCoreHttpClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public HttpClient GetHttpClient()
        {
            // The HttpClient will be created with the named client registered in the DI container
            // If the named client is not found, the factory will fall back to the default client
            HttpClient httpClient = _httpClientFactory.CreateClient("token_acquisition_httpclient");
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
            return httpClient;
        }
    }
}
