// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Interface implemented by diagnostics for the JwtBearer middleware.
    /// </summary>
    public interface IJwtBearerMiddlewareDiagnostics
    {
        /// <summary>
        /// Called to subscribe to JwtBearerEvents.
        /// </summary>
        /// <param name="events">JwtBearer events.</param>
        /// <returns>the events (for chaining).</returns>
        JwtBearerEvents Subscribe(JwtBearerEvents events);
    }
}
