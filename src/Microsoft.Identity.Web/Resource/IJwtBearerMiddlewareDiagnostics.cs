// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Interface implemented by diagnostics for the JWT Bearer middleware.
    /// </summary>
    public interface IJwtBearerMiddlewareDiagnostics
    {
        /// <summary>
        /// Called to subscribe to <see cref="JwtBearerEvents"/>.
        /// </summary>
        /// <param name="events">JWT Bearer events.</param>
        /// <returns>The events (for chaining).</returns>
        JwtBearerEvents Subscribe(JwtBearerEvents events);
    }
}
