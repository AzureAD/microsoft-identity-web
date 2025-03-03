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

        public MockHttpMessageHandler AddMockHandler(MockHttpMessageHandler handler)
        {
            if (_httpMessageHandlerQueue.Count == 0 )
            {
                handler.ReplaceMockHttpMessageHandler = AddMockHandler;
            }
            _httpMessageHandlerQueue.Enqueue(handler);
            return handler;
        }

        private Queue<HttpMessageHandler> _httpMessageHandlerQueue = new Queue<HttpMessageHandler>();

        public HttpClient GetHttpClient()
        {
            HttpMessageHandler messageHandler;

            Assert.NotEmpty(_httpMessageHandlerQueue);
            messageHandler = _httpMessageHandlerQueue.Dequeue();

            var httpClient = new HttpClient(messageHandler);

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return GetHttpClient();
        }
    }
}
