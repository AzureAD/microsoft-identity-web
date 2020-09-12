// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal class MsalAspNetCoreHttpClientFactory : IMsalHttpClientFactory
    {
        private IHttpClientFactory _httpClientFactory;

        public MsalAspNetCoreHttpClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public HttpClient GetHttpClient()
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, Constants.IDWebSku + IdHelper.GetIdWebVersion());
            return httpClient;
        }
    }
}
