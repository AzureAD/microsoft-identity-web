// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using System.Net.Http;

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
            return _httpClientFactory.CreateClient();
        }
    }
}
