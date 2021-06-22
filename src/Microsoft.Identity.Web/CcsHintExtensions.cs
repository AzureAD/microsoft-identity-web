// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Advanced;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension methods to send CCS headers.
    /// </summary>
    internal static class CcsRoutingHintExtensions
    {
        /// <summary>
        /// Sets the CCS routing hint.
        /// </summary>
        /// <typeparam name="T">Builder type.</typeparam>
        /// <param name="builder">Builder.</param>
        /// <param name="user">Claims principal for the user.</param>
        /// <returns>The builder to chain.</returns>
        public static AbstractAcquireTokenParameterBuilder<T> WithCcsRoutingHint<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            ClaimsPrincipal? user)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.WithExtraHttpHeaders(new Dictionary<string, string>
            {
                { Constants.XAnchorMailbox, CreateCcsRoutingHintFromHttpContext(user) },
            });

            return builder;
        }

        internal static string CreateCcsRoutingHintFromHttpContext(ClaimsPrincipal? user)
        {
            if (user != null)
            {
                string? oid = user.GetObjectId();
                string? tid = user.GetTenantId();
                if (!string.IsNullOrEmpty(oid) && !string.IsNullOrEmpty(tid))
                {
                    return $"oid:{oid}@{tid}";
                }
            }

            return string.Empty;
        }
    }
}
