// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using static Microsoft.Identity.Web.AppServicesAuthenticationTokenAcquisition;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extension class containing cookie policies (work around for same site).
    /// </summary>
    public static class CookiePolicyOptionsExtensions
    {
        private const int Two = 2;
        private const int SixtySeven = 67;
        private const int FiftyOne = 51;
        private const int Thirteen = 13;
        private const int Twelve = 12;
        private const int Ten = 10;
        private const int Fourteen = 14;

        /// <summary>
        /// Handles SameSite cookie issue according to the https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1.
        /// The default list of user agents that disallow "SameSite=None",
        /// was taken from https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/.
        /// </summary>
        /// <param name="options"><see cref="CookiePolicyOptions"/>to update.</param>
        /// <returns><see cref="CookiePolicyOptions"/> to chain.</returns>
        public static CookiePolicyOptions HandleSameSiteCookieCompatibility(this CookiePolicyOptions options)
        {
            return HandleSameSiteCookieCompatibility(options, DisallowsSameSiteNone);
        }

        /// <summary>
        /// Handles SameSite cookie issue according to the docs: https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
        /// The default list of user agents that disallow "SameSite=None", was taken from https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/.
        /// </summary>
        /// <param name="options"><see cref="CookiePolicyOptions"/>to update.</param>
        /// <param name="disallowsSameSiteNone">If you don't want to use the default user agent list implementation,
        /// the method sent in this parameter will be run against the user agent and if returned true, SameSite value will be set to Unspecified.
        /// The default user agent list used can be found at: https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/. </param>
        /// <returns><see cref="CookiePolicyOptions"/> to chain.</returns>
        public static CookiePolicyOptions HandleSameSiteCookieCompatibility(this CookiePolicyOptions options, Func<string, bool> disallowsSameSiteNone)
        {
            _ = Throws.IfNull(options);

            options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
            options.OnAppendCookie = cookieContext =>
                CheckSameSite(cookieContext.Context, cookieContext.CookieOptions, disallowsSameSiteNone);
            options.OnDeleteCookie = cookieContext =>
                CheckSameSite(cookieContext.Context, cookieContext.CookieOptions, disallowsSameSiteNone);

            return options;
        }

        private static void CheckSameSite(HttpContext httpContext, CookieOptions options, Func<string, bool> disallowsSameSiteNone)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers[Constants.UserAgent].ToString();
                if (disallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }

        /// <summary>
        /// Checks if the specified user agent supports "SameSite=None" cookies.
        /// </summary>
        /// <param name="userAgent">Browser user agent.</param>
        /// <remarks>
        /// Incompatible user agents include:
        /// <list type="bullet">
        /// <item>Versions of Chrome from Chrome 51 to Chrome 66 (inclusive on both ends).</item>
        /// <item>Versions of UC Browser on Android prior to version 12.13.2.</item>
        /// <item>Versions of Safari and embedded browsers on MacOS 10.14 and all browsers on iOS 12.</item>
        /// </list>
        /// Reference: https://www.chromium.org/updates/same-site/incompatible-clients.
        /// </remarks>
        /// <returns>True, if the user agent does not allow "SameSite=None" cookie; otherwise, false.</returns>
        public static bool DisallowsSameSiteNone(string userAgent)
        {
            return HasWebKitSameSiteBug() ||
                DropsUnrecognizedSameSiteCookies();

            bool HasWebKitSameSiteBug() =>
                IsIosVersion(Twelve) ||
                (IsMacosxVersion(Ten, Fourteen) &&
                (IsSafari() || IsMacEmbeddedBrowser()));

            bool DropsUnrecognizedSameSiteCookies()
            {
                if (IsUcBrowser())
                {
                    return !IsUcBrowserVersionAtLeast(Twelve, Thirteen, Two);
                }

                return IsChromiumBased() &&
                    IsChromiumVersionAtLeast(FiftyOne) &&
                    !IsChromiumVersionAtLeast(SixtySeven);
            }

            bool IsIosVersion(int major)
            {
                const string regex = @"\(iP.+; CPU .*OS (\d+)[_\d]*.*\) AppleWebKit\/";

                // Extract digits from first capturing group.
                Match match = Regex.Match(userAgent, regex);
                return match.Groups[1].Value == major.ToString(CultureInfo.CurrentCulture);
            }

            bool IsMacosxVersion(int major, int minor)
            {
                const string regex = @"\(Macintosh;.*Mac OS X (\d+)_(\d+)[_\d]*.*\) AppleWebKit\/";

                // Extract digits from first and second capturing groups.
                Match match = Regex.Match(userAgent, regex);
                return match.Groups[1].Value == major.ToString(CultureInfo.CurrentCulture) &&
                    match.Groups[Two].Value == minor.ToString(CultureInfo.CurrentCulture);
            }

            bool IsSafari()
            {
                const string regex = @"Version\/.* Safari\/";

                return Regex.IsMatch(userAgent, regex) &&
                       !IsChromiumBased();
            }

            bool IsMacEmbeddedBrowser()
            {
                const string regex = @"^Mozilla\/[\.\d]+ \(Macintosh;.*Mac OS X [_\d]+\) AppleWebKit\/[\.\d]+ \(KHTML, like Gecko\)$";

                return Regex.IsMatch(userAgent, regex);
            }

            bool IsChromiumBased()
            {
                const string regex = "Chrom(e|ium)";

                return Regex.IsMatch(userAgent, regex);
            }

            bool IsChromiumVersionAtLeast(int major)
            {
                const string regex = @"Chrom[^ \/]+\/(\d+)[\.\d]*";

                // Extract digits from first capturing group.
                Match match = Regex.Match(userAgent, regex);
                if (!match.Success)
                    return false;

                if (int.TryParse(match.Groups[1].Value, out int version))
                    return version >= major;

                return false;
            }

            bool IsUcBrowser()
            {
                const string regex = @"UCBrowser\/";

                return Regex.IsMatch(userAgent, regex);
            }

            bool IsUcBrowserVersionAtLeast(int major, int minor, int build)
            {
                const string regex = @"UCBrowser\/(\d+)\.(\d+)\.(\d+)[\.\d]* ";

                // Extract digits from three capturing groups.
                Match match = Regex.Match(userAgent, regex);
                int major_version = Convert.ToInt32(match.Groups[1].Value, CultureInfo.CurrentCulture);
                int minor_version = Convert.ToInt32(match.Groups[2].Value, CultureInfo.CurrentCulture);
                int build_version = Convert.ToInt32(match.Groups[3].Value, CultureInfo.CurrentCulture);
                if (major_version != major)
                {
                    return major_version > major;
                }

                if (minor_version != minor)
                {
                    return minor_version > minor;
                }

                return build_version >= build;
            }
        }
    }
}
