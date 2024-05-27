// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension class on the <see cref="IRequestOption"/>
    /// </summary>
    public static class RequestOptionsExtension
    {
        /// <summary>
        /// Specify the authentication options for the request.
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="optionsValue">Authentication request options to set.</param>
        /// <returns></returns>
        public static IList<IRequestOption> WithAuthenticationOptions(this IList<IRequestOption> options, GraphAuthenticationOptions optionsValue)
        {
            GraphAuthenticationOptions? graphAuthenticationOptions = options.OfType<GraphAuthenticationOptions>().FirstOrDefault();
            if (graphAuthenticationOptions != null)
            {
                throw new ArgumentException("Can't add GraphAuthenticationOptions twice. Rather use the delegate.");
            }
            options.Add(optionsValue);
            return options;
        }

        /// <summary>
        /// Specify a delegate setting the authentication options for the request.
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="optionsValue">Delegate setting the authentication request.</param>
        /// <returns></returns>
        public static IList<IRequestOption> WithAuthenticationOptions(this IList<IRequestOption> options, Action<GraphAuthenticationOptions> optionsValue)
        {
            GraphAuthenticationOptions? graphAuthenticationOptions = options.OfType<GraphAuthenticationOptions>().FirstOrDefault();
            if (graphAuthenticationOptions == null)
            {
                graphAuthenticationOptions = new GraphAuthenticationOptions();
                options.Add(graphAuthenticationOptions);
            }
            optionsValue(graphAuthenticationOptions);
            return options;
        }

        /// <summary>
        /// Specify the scopes to use to request a token for the request.
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="scopes">Microsoft Graph scopes used to authenticate this request.</param>
        /// <returns></returns>
        public static IList<IRequestOption> WithScopes(this IList<IRequestOption> options, params string[] scopes)
        {
            GraphAuthenticationOptions? graphAuthenticationOptions = options.OfType<GraphAuthenticationOptions>().FirstOrDefault();
            if (graphAuthenticationOptions == null)
            {
                graphAuthenticationOptions = new GraphAuthenticationOptions();
                options.Add(graphAuthenticationOptions);
            }
            graphAuthenticationOptions.Scopes = scopes;
            return options;
        }

        /// <summary>
        /// Specifies to use app only permissions for Graph.
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="appOnly">Should the permissions be app only or not.</param>
        /// <param name="tenant">Tenant ID or domain for which we want to make the call..</param>
        /// <returns></returns>
        public static IList<IRequestOption> WithAppOnly(this IList<IRequestOption> options, bool appOnly=true, string? tenant=null)
        {
            GraphAuthenticationOptions? graphAuthenticationOptions = options.OfType<GraphAuthenticationOptions>().FirstOrDefault();
            if (graphAuthenticationOptions == null)
            {
                graphAuthenticationOptions = new GraphAuthenticationOptions();
                options.Add(graphAuthenticationOptions);
            }
            graphAuthenticationOptions.RequestAppToken = appOnly;
            graphAuthenticationOptions.AcquireTokenOptions ??= new();
            graphAuthenticationOptions.AcquireTokenOptions.Tenant = tenant;
            return options;
        }

#if NETCOREAPP
        /// <summary>
        /// In ASP.NET Core, specify the authentication scheme used to authenticate this request.
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="authenticationScheme">ASP.NET Core authentication scheme used to authenticate this request.</param>
        /// <returns></returns>
        public static IList<IRequestOption> WithAuthenticationScheme(this IList<IRequestOption> options, string authenticationScheme)
        {
            GraphAuthenticationOptions? graphAuthenticationOptions = options.OfType<GraphAuthenticationOptions>().FirstOrDefault();
            if (graphAuthenticationOptions == null)
            {
                graphAuthenticationOptions = new GraphAuthenticationOptions();
                options.Add(graphAuthenticationOptions);
            }
            graphAuthenticationOptions.AcquireTokenOptions ??= new();
            graphAuthenticationOptions.AcquireTokenOptions.AuthenticationOptionsName = authenticationScheme;
            return options;
        }
#endif

        /// <summary>
        /// Specifies to use app only permissions for Graph.
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="user">Overrides the user on behalf of which Microsoft Graph is called
        /// (for delegated permissions in some specific scenarios)</param>
        /// <returns></returns>
        public static IList<IRequestOption> WithUser(this IList<IRequestOption> options, ClaimsPrincipal user)
        {
            GraphAuthenticationOptions? graphAuthenticationOptions = options.OfType<GraphAuthenticationOptions>().FirstOrDefault();
            if (graphAuthenticationOptions == null)
            {
                graphAuthenticationOptions = new GraphAuthenticationOptions();
                options.Add(graphAuthenticationOptions);
            }
            graphAuthenticationOptions.User = user;
            return options;
        }
    }
}
