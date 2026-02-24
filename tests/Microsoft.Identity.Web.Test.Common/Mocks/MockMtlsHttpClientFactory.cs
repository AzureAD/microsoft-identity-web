// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client;
using Xunit;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    /// <summary>
    /// HttpClient factory that serves Http responses for testing purposes and supports mTLS certificate binding.
    /// </summary>
    /// <remarks>
    /// This implements both IHttpClientFactory and IMsalMtlsHttpClientFactory for testing mTLS scenarios.
    /// </remarks>
    public class MockMtlsHttpClientFactory : IMsalMtlsHttpClientFactory, IHttpClientFactory, IDisposable
    {
        private LinkedList<MockHttpMessageHandler> _httpMessageHandlerQueue = new();

        private volatile bool _addInstanceDiscovery = true;

        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            if (_httpMessageHandlerQueue.Count == 0 && _addInstanceDiscovery)
            {
                _addInstanceDiscovery = false;
                handler.ReplaceMockHttpMessageHandler = (h) =>
                {
                    return _httpMessageHandlerQueue.AddFirst(h).Value;
                };
            }

            // add a message to the front of the queue
            _httpMessageHandlerQueue.AddLast(handler);
            return handler;
        }

        public HttpClient GetHttpClient()
        {
            HttpMessageHandler? messageHandler = _httpMessageHandlerQueue.First?.Value;
            if (messageHandler == null)
            {
                throw new InvalidOperationException("The mock HTTP message handler queue is empty.");
            }
            _httpMessageHandlerQueue.RemoveFirst();

            var httpClient = new HttpClient(messageHandler);

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return GetHttpClient();
        }

        /// <summary>
        /// Gets an HttpClient configured with the specified certificate for mTLS.
        /// </summary>
        /// <param name="certificate">The certificate to use for mTLS.</param>
        /// <returns>An HttpClient configured for mTLS.</returns>
        public HttpClient GetHttpClient(X509Certificate2 certificate)
        {
            // For testing purposes, return the same mocked HttpClient regardless of certificate
            // In a real implementation, this would configure the HttpClient with the certificate
            return GetHttpClient();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // This ensures we only check the mock queue on dispose when we're not in the middle of an
            // exception flow.  Otherwise, any early assertion will cause this to likely fail
            // even though it's not the root cause.
#pragma warning disable CS0618 // Type or member is obsolete - this is non-production code so it's fine
            if (Marshal.GetExceptionCode() == 0)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                string remainingMocks = string.Join(
                    " ",
                    _httpMessageHandlerQueue.Select(
                        h => (h as MockHttpMessageHandler)?.ExpectedUrl ?? string.Empty));

                Assert.Empty(_httpMessageHandlerQueue);
            }
        }
    }
}
