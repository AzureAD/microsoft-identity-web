// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Identifiers used for switching between different app behaviors within the library.
    /// </summary>
    /// <remarks>
    /// This library uses <see cref="System.AppContext" /> to turn on or off certain API behavioral
    /// changes that might have an effect on application compatibility. This class defines the set of switches that are
    /// available to modify library behavior. Setting a switch's value can be
    /// done programmatically through the <see cref="System.AppContext.SetSwitch" /> method, or through other means such as
    /// setting it through MSBuild, app configuration, or registry settings. These alternate methods are described in the
    /// <see cref="System.AppContext.SetSwitch" /> documentation.
    /// </remarks>
    internal static class AppContextSwitches
    {
        /// <summary>
        /// Enables a fallback to the previous behavior of using <see cref="ClaimsIdentity"/> instead of <see cref="CaseSensitiveClaimsIdentity"/> globally.
        /// </summary>
        internal const string UseClaimsIdentityTypeSwitchName = "Microsoft.IdentityModel.Tokens.UseClaimsIdentityType";

        private static bool? s_useClaimsIdentityType;

        internal static bool UseClaimsIdentityType => s_useClaimsIdentityType ??= (AppContext.TryGetSwitch(UseClaimsIdentityTypeSwitchName, out bool useClaimsIdentityType) && useClaimsIdentityType);

        /// <summary>
        /// Used for testing to reset all switches to its default value.
        /// </summary>
        internal static void ResetState()
        {
            AppContext.SetSwitch(UseClaimsIdentityTypeSwitchName, false);
            s_useClaimsIdentityType = null;
        }
    }
}
