// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    public class MockHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext HttpContext { get => new DefaultHttpContext(); set => new DefaultHttpContext(); }
    }
}
