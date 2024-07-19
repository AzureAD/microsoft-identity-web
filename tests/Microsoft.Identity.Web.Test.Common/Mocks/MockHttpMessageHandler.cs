// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<MockHttpMessageHandler, MockHttpMessageHandler> ReplaceMockHttpMessageHandler { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MockHttpMessageHandler()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {

        }
        public HttpResponseMessage ResponseMessage { get; set; }

        public string ExpectedUrl { get; set; }

        public HttpMethod ExpectedMethod { get; set; }

        public Exception ExceptionToThrow { get; set; }

        /// <summary>
        /// Once the http message is executed, this property holds the request message.
        /// </summary>
        public HttpRequestMessage ActualRequestMessage { get; private set; }
        public Dictionary<string, string> ActualRequestPostData { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ActualRequestMessage = request;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            var uri = request.RequestUri;
            Assert.NotNull(uri);

            //Intercept instance discovery requests and serve a response. 
            //Also, requeue the current mock handler for MSAL's next request.
#if NET6_0_OR_GREATER
            if (uri.AbsoluteUri.Contains("/discovery/instance", StringComparison.OrdinalIgnoreCase))
#else
            if (uri.AbsoluteUri.Contains("/discovery/instance"))
#endif
            {
                if (ReplaceMockHttpMessageHandler != null)
                {
                    ReplaceMockHttpMessageHandler(this);

                    var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(TestConstants.DiscoveryJsonResponse),
                    };

                    return responseMessage;
                }
            }

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
                string postData = await request.Content.ReadAsStringAsync();
                ActualRequestPostData = QueryStringParser.ParseKeyValueList(postData, '&', true, false);

            }

            return ResponseMessage;
        }
    }
}
