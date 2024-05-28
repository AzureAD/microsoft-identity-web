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
            // The HttpClient will be created with the name of the client registered in the DI container
            // If the named client(dSTS) is not found, the factory will fall back to the default client
            HttpClient httpClient = _httpClientFactory.CreateClient("dSTS");
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
            return httpClient;
        }
    }
}
