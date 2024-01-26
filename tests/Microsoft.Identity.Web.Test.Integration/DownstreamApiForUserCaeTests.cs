// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Lab.Api;
using Xunit;

namespace Microsoft.Identity.Web.Test.Integration
{
    public class DownstreamApiForUserCaeTests
    {
        private readonly TimeSpan _delayTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Login with a user; the cache should have a user token.
        /// Call DownstreamApi.GetForUserAsync to Graph's /me endpoint. Internally:
        /// - Gets a token from AAD.
        /// - Calls Graph with the token; should receive a successful response.
        /// Revoke the user's session and wait until the changes propagate to Graph.
        /// Call DownstreamApi.GetForUserAsync again. Internally:
        /// - Gets a token from cache.
        /// - Calls Graph again; should receive a 401 with claims.
        /// - Tries to acquire a new token with claims.
        /// - Triggers MsalUiRequiredEx flow; user resigns in.
        /// Redirects to DownstreamApi.GetForUserAsync again. Internally:
        /// - Gets a token from cache.
        /// - Calls Graph again; should receive a successful response.
        /// </summary>
        [Fact]
        public void UserFlow_WithSessionRevoked_ThrowsMsalUiRequiredException()
        {
            throw new NotImplementedException();
            //var result1 = await _downstreamApi.GetForUserAsync<EmptyClass>("GraphUser",
            //    options => options.RelativePath = "me");
        }

        // Placeholder for a generic type
        private class EmptyClass { }
    }
}
