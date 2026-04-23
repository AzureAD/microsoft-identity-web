// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.Hosts;

namespace Microsoft.Identity.Web.OWIN
{
    /// <summary>
    /// Token acquirer factory for OWIN web apps and web APIs.
    /// </summary>
    public class OwinTokenAcquirerFactory : TokenAcquirerFactory 
    {
        /// <summary>
        /// Defines the configuration for a given host.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected override string DefineConfiguration(IConfigurationBuilder builder)
        {
            _ = builder.AddInMemoryCollection(new Dictionary<string, string?>()
            {
                ["AzureAd:Instance"] = EnsureTrailingSlash(System.Configuration.ConfigurationManager.AppSettings["ida:Instance"] ?? System.Configuration.ConfigurationManager.AppSettings["ida:AADInstance"] ?? "https://login.microsoftonline.com/"),
                ["AzureAd:ClientId"] = System.Configuration.ConfigurationManager.AppSettings["ida:ClientId"],
                ["AzureAd:TenantId"] = System.Configuration.ConfigurationManager.AppSettings["ida:Tenant"] ?? System.Configuration.ConfigurationManager.AppSettings["ida:TenantId"],
                ["AzureAd:Audience"] = System.Configuration.ConfigurationManager.AppSettings["ida:Audience"],
                ["AzureAd:ClientSecret"] = System.Configuration.ConfigurationManager.AppSettings["ida:ClientSecret"],
                ["AzureAd:SignedOutCallbackPath"] = System.Configuration.ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"],
                ["AzureAd:RedirectUri"] = System.Configuration.ConfigurationManager.AppSettings["ida:RedirectUri"],
            });

            return HostingEnvironment.MapPath("~/");
        }

        /// <summary>
        /// Pre-build action. Ensures that the host is an OWIN host.
        /// </summary>
        protected override void PreBuild()
        {
            base.PreBuild();

            // Replace the genenric host by an OWIN host
            ServiceDescriptor? tokenAcquisitionhost = Services.FirstOrDefault(s => s.ServiceType == typeof(ITokenAcquisitionHost));
            if (tokenAcquisitionhost != null)
            {
                Services.Remove(tokenAcquisitionhost);

                if (tokenAcquisitionhost.Lifetime == ServiceLifetime.Singleton)
                {
                    // The service was already added, but not with the right lifetime
                    Services.AddSingleton<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
                else
                {
                    // The service is already added with the right lifetime
                    Services.AddScoped<ITokenAcquisitionHost, OwinTokenAcquisitionHost>();
                }
            }

        }

        private static string EnsureTrailingSlash(string value)
        {
            value ??= string.Empty;

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}
