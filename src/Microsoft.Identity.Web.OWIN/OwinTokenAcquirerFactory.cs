// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Web.OWIN
{
    internal class OwinTokenAcquirerFactory : TokenAcquirerFactory 
    {
        protected override string DefineConfiguration(IConfigurationBuilder builder)
        {
            IConfigurationBuilder configurationBuilder = builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["AzureAd:Instance"] = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:Instance"] ?? ConfigurationManager.AppSettings["ida:AADInstance"] ?? "https://login.microsoftonline.com/"),
                ["AzureAd:ClientId"] = ConfigurationManager.AppSettings["ida:ClientId"],
                ["AzureAd:TenantId"] = ConfigurationManager.AppSettings["ida:Tenant"] ?? ConfigurationManager.AppSettings["ida:TenantId"],
                ["AzureAd:Audience"] = ConfigurationManager.AppSettings["ida:Audience"],
                ["AzureAd:ClientSecret"] = ConfigurationManager.AppSettings["ida:ClientSecret"],
                ["AzureAd:SignedOutCallbackPath"] = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"],
                ["AzureAd:RedirectUri"] = ConfigurationManager.AppSettings["ida:RedirectUri"],
            });
            return HttpContext.Current.Request.PhysicalApplicationPath;
        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}
