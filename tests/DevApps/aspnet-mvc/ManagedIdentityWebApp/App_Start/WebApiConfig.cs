// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Web.Http;

namespace ManagedIdentityWebApp;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        // Attribute routing (e.g. [Route("AppService")]).
        config.MapHttpAttributeRoutes();
    }
}
