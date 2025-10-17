// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Provides a factory for creating HTTP clients configured for mTLS authentication with using binding certificate.
    /// It uses a hybrid approach with leveraging IHttpClientFactory for non-mTLS HTTL clients and maintaining
    /// a pool of mTLS clients with using certificate as a key.
    /// </summary>
    internal sealed class MsalMtlsHttpClientFactory : IMsalMtlsHttpClientFactory
    {
        private const long MaxMtlsHttpClientCountInPool = 1000;
        private const long MaxResponseContentBufferSizeInBytes = 1024 * 1024;

        // Please see (https://aka.ms/msal-httpclient-info) for important information regarding the HttpClient.
        private static readonly ConcurrentDictionary<string, HttpClient> s_mtlsHttpClientPool = new ConcurrentDictionary<string, HttpClient>();
        private static readonly object s_cacheLock = new object();

        private readonly IHttpClientFactory _httpClientFactory;

        public MsalMtlsHttpClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public HttpClient GetHttpClient()
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
            return httpClient;
        }

        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 == null)
            {
                return GetHttpClient();
            }

            string key = x509Certificate2.Thumbprint;
            HttpClient httpClient = CreateMtlsHttpClient(x509Certificate2);
            httpClient = s_mtlsHttpClientPool.GetOrAdd(key, httpClient);
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
            return httpClient;
        }

        private HttpClient CreateMtlsHttpClient(X509Certificate2 bindingCertificate)
        {
#if SUPPORTS_MTLS
            CheckAndManageCache();

            if (bindingCertificate == null)
            {
                throw new ArgumentNullException(nameof(bindingCertificate), "A valid X509 certificate must be provided for mTLS.");
            }

            HttpClientHandler handler = new();
            handler.ClientCertificates.Add(bindingCertificate);

            // HTTP client factory can't be used there because HTTP client handler needs to be configured
            // before a HTTP client instance is created
            var httpClient = new HttpClient(handler);
            ConfigureRequestHeadersAndSize(httpClient);
            return httpClient;
#else
        throw new NotSupportedException("mTLS is not supported on this platform.");
#endif
        }

        private static void CheckAndManageCache()
        {
            lock (s_cacheLock)
            {
                if (s_mtlsHttpClientPool.Count >= 1000)
                {
                    s_mtlsHttpClientPool.Clear();
                }
            }
        }

        private static void ConfigureRequestHeadersAndSize(HttpClient httpClient)
        {
            httpClient.MaxResponseContentBufferSize = MaxResponseContentBufferSizeInBytes;
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
