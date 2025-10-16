// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal sealed class MsalMtlsHttpClientFactory : IMsalMtlsHttpClientFactory
    {
        private readonly IMsalMtlsHttpClientFactory _httpClientFactory;

        public MsalMtlsHttpClientFactory(IMsalMtlsHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public HttpClient GetHttpClient()
        {
            HttpClient httpClient = _httpClientFactory.GetHttpClient();
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
            return httpClient;
        }

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            HttpClient httpClient = _httpClientFactory.GetHttpClient(x509Certificate2);
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
            return httpClient;
        }
    }
}
