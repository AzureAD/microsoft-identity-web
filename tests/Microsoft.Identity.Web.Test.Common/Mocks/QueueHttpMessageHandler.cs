// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    /// <summary>
    /// Serves predefined HTTP responses.
    /// </summary>
    public class QueueHttpMessageHandler : HttpMessageHandler
    {
        private readonly ConcurrentQueue<HttpResponseMessage> _httpResponseMessageQueue = new();
        private Func<HttpResponseMessage>? _httpResponseMessageProvider;

        public QueueHttpMessageHandler(Func<HttpResponseMessage>? httpResponseMessageProvider = null)
        {
            _httpResponseMessageProvider = httpResponseMessageProvider;
        }

        public void AddHttpResponseMessage(HttpResponseMessage message) => _httpResponseMessageQueue.Enqueue(message);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_httpResponseMessageProvider != null)
            {
                return Task.FromResult(_httpResponseMessageProvider());
            }

            HttpResponseMessage? httpResponseMessage;
            if (!_httpResponseMessageQueue.TryDequeue(out httpResponseMessage) || httpResponseMessage == null)
            {
                Assert.Fail("The HTTP Response Message queue is empty. Cannot serve another response.");
            }

            return Task.FromResult(httpResponseMessage);
        }

        protected override void Dispose(bool disposing)
        {
            // All responses should have been used up.
            Assert.Empty(_httpResponseMessageQueue);
            if (!disposing)
            {
                base.Dispose(false);
            }
        }
    }
}
