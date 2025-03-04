// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;
using NSubstitute.Routing.Handlers;
using Xunit;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    /// <summary>
    /// HttpClient that serves Http responses for testing purposes. Instance Discovery is added by default.
    /// </summary>
    /// <remarks>
    /// This implements the both IHttpClientFactory, which is what ID.Web uses. And IMsalHttpClientFactory which is what MSAL uses.
    /// </remarks>
    public class MockHttpClientFactory : IMsalHttpClientFactory, IHttpClientFactory, IDisposable
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
