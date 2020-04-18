// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Diagnostics used in the Open Id Connect middleware
    /// (used in Web Apps).
    /// </summary>
    public interface IOpenIdConnectMiddlewareDiagnostics
    {
        void Subscribe(OpenIdConnectEvents events);
    }
}
