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
        private string _automaticJsonConfigurationVirtualPath = "~/bin";

        /// <summary>
        /// Defines the configuration for a given host.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected override string DefineConfiguration(IConfigurationBuilder builder)
        {
            _ = builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                ["AzureAd:Instance"] = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:Instance"] ?? ConfigurationManager.AppSettings["ida:AADInstance"] ?? "https://login.microsoftonline.com/"),
                ["AzureAd:ClientId"] = ConfigurationManager.AppSettings["ida:ClientId"],
                ["AzureAd:TenantId"] = ConfigurationManager.AppSettings["ida:Tenant"] ?? ConfigurationManager.AppSettings["ida:TenantId"],
                ["AzureAd:Audience"] = ConfigurationManager.AppSettings["ida:Audience"],
                ["AzureAd:ClientSecret"] = ConfigurationManager.AppSettings["ida:ClientSecret"],
                ["AzureAd:SignedOutCallbackPath"] = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"],
                ["AzureAd:RedirectUri"] = ConfigurationManager.AppSettings["ida:RedirectUri"],
            });

            return ResolveConfigurationBasePath(
                ConfigurationManager.AppSettings[UseLegacyWebRootAppSettings],
                HostingEnvironment.MapPath,
                File.Exists,
                out _automaticJsonConfigurationVirtualPath);
        }

        /// <inheritdoc/>
        protected override void AddDefaultJsonConfiguration(
            IConfigurationBuilder builder,
            string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return;
            }

            try
            {
                base.AddDefaultJsonConfiguration(builder, basePath);
            }
            catch (DirectoryNotFoundException)
            {
                WarnAutomaticJsonConfigurationSkipped(_automaticJsonConfigurationVirtualPath);
            }
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
            Func<string, bool> fileExists,
            out string virtualPath)
        {
            virtualPath = "~/bin";
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
                            "Application-root loading was requested. Move appsettings.json to the bin " +
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

            string? basePath;
            try
            {
                basePath = mapPath(virtualPath);
            }
            catch (ArgumentException)
            {
                WarnAutomaticJsonConfigurationSkipped(virtualPath);
                return string.Empty;
            }
            catch (HttpException)
            {
                WarnAutomaticJsonConfigurationSkipped(virtualPath);
                return string.Empty;
            }
            catch (InvalidOperationException)
            {
                WarnAutomaticJsonConfigurationSkipped(virtualPath);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(basePath))
            {
                WarnAutomaticJsonConfigurationSkipped(virtualPath);
                return string.Empty;
            }

            return basePath!;
        }

        private static void WarnAutomaticJsonConfigurationSkipped(string virtualPath)
        {
            if (string.Equals(virtualPath, "~/", StringComparison.Ordinal))
            {
                Trace.TraceWarning(
                    $"Legacy JSON loading was requested by '{UseLegacyWebRootAppSettings}=true', " +
                    "but the application's '~/' location is unavailable. Automatic JSON configuration was skipped. " +
                    "Web.config and environment-variable settings remain available. If JSON settings are required, " +
                    "publish them under '~/bin', disable the legacy setting, and restart the application.");
            }
            else
            {
                Trace.TraceWarning(
                    "Automatic JSON configuration was skipped because the application's '~/bin' location is unavailable. " +
                    "Web.config and environment-variable settings remain available. If JSON settings are required, " +
                    "repair the deployment, publish appsettings.json under '~/bin', and restart the application.");
            }
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
