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
        private readonly bool _ignoreInstanceDiscovery;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MockHttpMessageHandler(bool ignoreInstanceDiscovery = true)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _ignoreInstanceDiscovery = ignoreInstanceDiscovery;
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
            var uri = request.RequestUri;

            ActualRequestMessage = request;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            Assert.NotNull(uri);

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
