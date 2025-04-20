// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Session;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Authentication builder returned by the EnableTokenAcquisitionToCallDownstreamApi methods
    /// enabling you to use the session cache implementation.
    /// </summary>
    public static class MicrosoftIdentityAppCallsWebApiAuthenticationBuilderExtension
    {
        /// <summary>
        /// Add a token cache based on session cookies
        /// </summary>
        /// <param name="builder"></param>
        /// <returns>The service collection</returns>
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
        [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(IConfiguration, Object)")]
#endif
        public static IServiceCollection AddSessionTokenCaches(this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder)
        {
            _ = Throws.IfNull(builder);
            // Add session if you are planning to use session based token cache
            var sessionStoreService = builder.Services.FirstOrDefault(x => x.ServiceType.Name == Constants.ISessionStore);

            // If not added already
            if (sessionStoreService == null)
            {
                builder.Services.AddSession(option =>
                {
                    option.Cookie.IsEssential = true;
                });
            }
            else
            {
                // If already added, ensure the options are set to use Cookies
                builder.Services.Configure<SessionOptions>(option =>
                {
                    option.Cookie.IsEssential = true;
                });
            }

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IMsalTokenCacheProvider, MsalSessionTokenCacheProvider>();
            builder.Services.TryAddScoped(provider =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
                if (httpContext == null)
                {
                    throw new InvalidOperationException(IDWebErrorMessage.HttpContextIsNull);
                }

                return httpContext.Session;
            });

            return builder.Services;
        }
    }
}
