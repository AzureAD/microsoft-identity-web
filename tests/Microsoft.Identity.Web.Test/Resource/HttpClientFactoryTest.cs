// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.Identity.Web.Test.Resource
{
    public class HttpClientFactoryTest : IHttpClientFactory
    {
        public Dictionary<string, HttpClient> dictionary = new Dictionary<string, HttpClient>();

        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
