// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseMessage { get; set; }
        public string ExpectedUrl { get; set; }

        public HttpMethod ExpectedMethod { get; set; }

        public Exception ExceptionToThrow { get; set; }

        /// <summary>
        /// Once the http message is executed, this property holds the request message
        /// </summary>
        public HttpRequestMessage ActualRequestMessage { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ActualRequestMessage = request;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            var uri = request.RequestUri;
            if (!string.IsNullOrEmpty(ExpectedUrl))
            {
                Assert.Equal(
                    ExpectedUrl,
                    uri.AbsoluteUri.Split(
                        new[]
                        {
                            '?',
                        })[0]);
            }

            Assert.Equal(ExpectedMethod, request.Method);

            if (request.Method != HttpMethod.Get && request.Content != null)
            {
                string postData = request.Content.ReadAsStringAsync().Result;
            }

            return new TaskFactory().StartNew(() => ResponseMessage, cancellationToken);
        }
    }
}
