// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Web.Http;
using Microsoft.Identity.Web;

namespace ManagedIdentityWebApp;

/// <summary>
/// Minimal .NET Framework (net48) web app that exercises Microsoft.Identity.Web's
/// App Service managed identity token acquisition. The <see cref="Microsoft.Identity.Abstractions.IAuthorizationHeaderProvider"/>
/// is resolved from a <see cref="TokenAcquirerFactory"/> built once at startup.
/// </summary>
public class WebApiApplication : System.Web.HttpApplication
{
    /// <summary>
    /// Service provider built from the default <see cref="TokenAcquirerFactory"/>, used by controllers
    /// to resolve Microsoft.Identity.Web services.
    /// </summary>
    public static IServiceProvider ServiceProvider { get; private set; }

    protected void Application_Start()
    {
        TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
        ServiceProvider = tokenAcquirerFactory.Build();

        GlobalConfiguration.Configure(WebApiConfig.Register);
    }
}
