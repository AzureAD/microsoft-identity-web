// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Identity.Web.MicrosoftGraph5
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
            options.Add(optionsValue);
            return options;
        }

        /// <summary>
        /// Speciy a delegate setting the authentication options for the request.
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="optionsValue">Delegate setting the authentication request.</param>
        /// <returns></returns>
        public static IList<IRequestOption> WithAuthenticationOptions(this IList<IRequestOption> options, Action<GraphAuthenticationOptions> optionsValue)
        {
            GraphAuthenticationOptions authorizationOptions = new GraphAuthenticationOptions();
            optionsValue(authorizationOptions);
            options.Add(authorizationOptions);
            return options;
        }


    }
}
