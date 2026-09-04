// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
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
        private const string UseLegacyWebRootAppSettings = "ida:UseLegacyWebRootAppSettings";

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

            return ResolveConfigurationBasePath(
                System.Configuration.ConfigurationManager.AppSettings[UseLegacyWebRootAppSettings],
                HostingEnvironment.MapPath,
                File.Exists);
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

        internal static string ResolveConfigurationBasePath(
            string? useLegacyWebRootAppSettings,
            Func<string, string?> mapPath,
            Func<string, bool> fileExists)
        {
            string virtualPath = "~/bin";
            bool useLegacyWebRoot = false;

            if (!string.IsNullOrEmpty(useLegacyWebRootAppSettings))
            {
                if (bool.TryParse(useLegacyWebRootAppSettings, out useLegacyWebRoot))
                {
                    if (useLegacyWebRoot)
                    {
                        virtualPath = "~/";
                        Trace.TraceWarning(
                            $"The temporary '{UseLegacyWebRootAppSettings}' compatibility setting is enabled. " +
                            "appsettings.json is being loaded from the application root. Move the file to the bin " +
                            "directory and remove this setting. The setting will be removed in a future major release.");
                    }
                }
                else
                {
                    Trace.TraceWarning(
                        $"The '{UseLegacyWebRootAppSettings}' appSetting must be 'true' or 'false'. " +
                        "The value was ignored and appsettings.json will be loaded from the bin directory.");
                }
            }

            if (!useLegacyWebRoot)
            {
                WarnIfRootAppSettingsExists(mapPath, fileExists);
            }

            string? basePath = mapPath(virtualPath);
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ConfigurationErrorsException(
                    $"Unable to resolve the OWIN configuration directory '{virtualPath}'. " +
                    "HostingEnvironment.MapPath returned no path.");
            }

            return basePath!;
        }

        private static void WarnIfRootAppSettingsExists(
            Func<string, string?> mapPath,
            Func<string, bool> fileExists)
        {
            string? rootAppSettingsPath;
            try
            {
                rootAppSettingsPath = mapPath("~/appsettings.json");
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (HttpException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(rootAppSettingsPath))
            {
                return;
            }

            bool rootAppSettingsExists;
            try
            {
                rootAppSettingsExists = fileExists(rootAppSettingsPath!);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (NotSupportedException)
            {
                return;
            }
            catch (SecurityException)
            {
                return;
            }

            if (rootAppSettingsExists)
            {
                Trace.TraceWarning(
                    "A file named 'appsettings.json' was detected in the application root. " +
                    "This version does not load that file by default. Publish configuration intended " +
                    "for Microsoft.Identity.Web under the application's bin directory, remove unnecessary " +
                    "root copies, and ensure the web server does not expose configuration files.");
            }
        }
    }
}
