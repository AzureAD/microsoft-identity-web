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
    /// It uses a hybrid approach with leveraging IHttpClientFactory for non-mTLS HTTP clients and maintaining
    /// a pool of mTLS clients with using certificate thumbprint as a key.
    /// </summary>
    public sealed class MsalMtlsHttpClientFactory : IMsalMtlsHttpClientFactory
    {
        private const long MaxMtlsHttpClientCountInPool = 1000;
        private const long MaxResponseContentBufferSizeInBytes = 1024 * 1024;

        // Please see (https://aka.ms/msal-httpclient-info) for important information regarding the HttpClient.
        private static readonly ConcurrentDictionary<string, HttpClient> s_mtlsHttpClientPool = new ConcurrentDictionary<string, HttpClient>();
        private static readonly object s_cacheLock = new object();

        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the MsalMtlsHttpClientFactory class using the specified HTTP client factory.
        /// </summary>
        /// <param name="httpClientFactory">The factory used to create HttpClient instances for mutual TLS (mTLS) operations. Cannot be null.</param>
        public MsalMtlsHttpClientFactory(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Creates and configures a new instance of <see cref="HttpClient"/> with telemetry headers applied.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="HttpClient"/> includes a telemetry header for tracking or
        /// diagnostics purposes. Callers are responsible for disposing the <see cref="HttpClient"/> instance when it is
        /// no longer needed.
        /// </remarks>
        /// <returns>A new <see cref="HttpClient"/> instance with telemetry information included in the default request headers.</returns>
        public HttpClient GetHttpClient()
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
            return httpClient;
        }

        /// <summary>
        /// Returns an instance of <see cref="HttpClient"/> configured to use the specified X.509 client certificate for
        /// mutual TLS authentication.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="HttpClient"/> instance is pooled and reused for the given certificate.
        /// The client includes a telemetry header in each request. Callers should not modify the default
        /// request headers or dispose the returned instance.
        /// </remarks>
        /// <param name="x509Certificate2">The X.509 certificate to use for client authentication. If <see langword="null"/>, a default <see cref="HttpClient"/> instance without client certificate authentication is returned.</param>
        /// <returns>A <see cref="HttpClient"/> instance configured for mutual TLS authentication using the specified certificate or default <see cref="HttpClient"/> instance.</returns>
        public HttpClient GetHttpClient(X509Certificate2 x509Certificate2)
        {
            if (x509Certificate2 == null)
            {
                return GetHttpClient();
            }

            string key = x509Certificate2.Thumbprint;
            HttpClient httpClient = CreateMtlsHttpClient(x509Certificate2);
            httpClient = s_mtlsHttpClientPool.GetOrAdd(key, httpClient);
            return httpClient;
        }

        private HttpClient CreateMtlsHttpClient(X509Certificate2 bindingCertificate)
        {
#if NET462
            throw new NotSupportedException("mTLS is not supported on this platform.");
#else
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
#endif
        }

        private static void CheckAndManageCache()
        {
            lock (s_cacheLock)
            {
                if (s_mtlsHttpClientPool.Count >= MaxMtlsHttpClientCountInPool)
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
            httpClient.DefaultRequestHeaders.Add(Constants.TelemetryHeaderKey, IdHelper.CreateTelemetryInfo());
        }
    }
}
