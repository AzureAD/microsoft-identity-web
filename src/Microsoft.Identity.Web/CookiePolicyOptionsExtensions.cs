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
    public static partial class CookiePolicyOptionsExtensions
    {
        private const int Two = 2;
        private const int SixtySeven = 67;
        private const int FiftyOne = 51;
        private const int Thirteen = 13;
        private const int Twelve = 12;
        private const int Ten = 10;
        private const int Fourteen = 14;
        private const int MaxUserAgentLength = 512;

#if !NET7_0_OR_GREATER
        private static readonly TimeSpan UserAgentRegexTimeout = TimeSpan.FromMilliseconds(100);
        private const RegexOptions UserAgentRegexOptions = RegexOptions.None;
#endif

        /// <summary>
        /// Handles SameSite cookies according to the ASP.NET Core documentation at https://learn.microsoft.com/aspnet/core/security/samesite.
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
        /// Handles SameSite cookies according to the ASP.NET Core documentation at https://learn.microsoft.com/aspnet/core/security/samesite.
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

                if (userAgent.Length > MaxUserAgentLength)
                {
                    return;
                }

                if (disallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex(@"\(iP.+; CPU .*OS (\d+)(?:_\d+)*[^)]*\) AppleWebKit\/", RegexOptions.NonBacktracking)]
        private static partial Regex IosVersionRegex();

        [GeneratedRegex(@"\(Macintosh;.*Mac OS X (\d+)_(\d+)(?:_\d+)*[^)]*\) AppleWebKit\/", RegexOptions.NonBacktracking)]
        private static partial Regex MacosxVersionRegex();

        [GeneratedRegex(@"Version\/.* Safari\/", RegexOptions.NonBacktracking)]
        private static partial Regex SafariRegex();

        [GeneratedRegex(@"^Mozilla\/[\.\d]+ \(Macintosh;.*Mac OS X [_\d]+\) AppleWebKit\/[\.\d]+ \(KHTML, like Gecko\)$", RegexOptions.NonBacktracking)]
        private static partial Regex MacEmbeddedBrowserRegex();

        [GeneratedRegex("Chrom(e|ium)", RegexOptions.NonBacktracking)]
        private static partial Regex ChromiumBasedRegex();

        [GeneratedRegex(@"Chrom[^ \/]+\/(\d+)[\.\d]*", RegexOptions.NonBacktracking)]
        private static partial Regex ChromiumVersionRegex();

        [GeneratedRegex(@"UCBrowser\/", RegexOptions.NonBacktracking)]
        private static partial Regex UcBrowserRegex();

        [GeneratedRegex(@"UCBrowser\/(\d+)\.(\d+)\.(\d+)[\.\d]* ", RegexOptions.NonBacktracking)]
        private static partial Regex UcBrowserVersionRegex();
#endif

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
            try
            {
                return HasWebKitSameSiteBug() ||
                    DropsUnrecognizedSameSiteCookies();
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

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
#if NET7_0_OR_GREATER
                Match match = IosVersionRegex().Match(userAgent);
#else
                Match match = Regex.Match(userAgent, @"\(iP.+; CPU .*OS (\d+)(?:_\d+)*[^)]*\) AppleWebKit\/", UserAgentRegexOptions, UserAgentRegexTimeout);
#endif
                return match.Groups[1].Value == major.ToString(CultureInfo.CurrentCulture);
            }

            bool IsMacosxVersion(int major, int minor)
            {
#if NET7_0_OR_GREATER
                Match match = MacosxVersionRegex().Match(userAgent);
#else
                Match match = Regex.Match(userAgent, @"\(Macintosh;.*Mac OS X (\d+)_(\d+)(?:_\d+)*[^)]*\) AppleWebKit\/", UserAgentRegexOptions, UserAgentRegexTimeout);
#endif
                return match.Groups[1].Value == major.ToString(CultureInfo.CurrentCulture) &&
                    match.Groups[Two].Value == minor.ToString(CultureInfo.CurrentCulture);
            }

            bool IsSafari()
            {
#if NET7_0_OR_GREATER
                return SafariRegex().IsMatch(userAgent) && !IsChromiumBased();
#else
                return Regex.IsMatch(userAgent, @"Version\/.* Safari\/", UserAgentRegexOptions, UserAgentRegexTimeout) &&
                       !IsChromiumBased();
#endif
            }

            bool IsMacEmbeddedBrowser()
            {
#if NET7_0_OR_GREATER
                return MacEmbeddedBrowserRegex().IsMatch(userAgent);
#else
                return Regex.IsMatch(userAgent, @"^Mozilla\/[\.\d]+ \(Macintosh;.*Mac OS X [_\d]+\) AppleWebKit\/[\.\d]+ \(KHTML, like Gecko\)$", UserAgentRegexOptions, UserAgentRegexTimeout);
#endif
            }

            bool IsChromiumBased()
            {
#if NET7_0_OR_GREATER
                return ChromiumBasedRegex().IsMatch(userAgent);
#else
                return Regex.IsMatch(userAgent, "Chrom(e|ium)", UserAgentRegexOptions, UserAgentRegexTimeout);
#endif
            }

            bool IsChromiumVersionAtLeast(int major)
            {
#if NET7_0_OR_GREATER
                Match match = ChromiumVersionRegex().Match(userAgent);
#else
                Match match = Regex.Match(userAgent, @"Chrom[^ \/]+\/(\d+)[\.\d]*", UserAgentRegexOptions, UserAgentRegexTimeout);
#endif
                if (!match.Success)
                    return false;

                if (int.TryParse(match.Groups[1].Value, out int version))
                    return version >= major;

                return false;
            }

            bool IsUcBrowser()
            {
#if NET7_0_OR_GREATER
                return UcBrowserRegex().IsMatch(userAgent);
#else
                return Regex.IsMatch(userAgent, @"UCBrowser\/", UserAgentRegexOptions, UserAgentRegexTimeout);
#endif
            }

            bool IsUcBrowserVersionAtLeast(int major, int minor, int build)
            {
#if NET7_0_OR_GREATER
                Match match = UcBrowserVersionRegex().Match(userAgent);
#else
                Match match = Regex.Match(userAgent, @"UCBrowser\/(\d+)\.(\d+)\.(\d+)[\.\d]* ", UserAgentRegexOptions, UserAgentRegexTimeout);
#endif
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
