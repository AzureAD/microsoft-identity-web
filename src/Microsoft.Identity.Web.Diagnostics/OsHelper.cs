// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Diagnostics
{
    internal class OsHelper
    {
        /// <summary>
        ///  Is this a windows platform
        /// </summary>
        /// <returns>A  value indicating if we are running on windows or not</returns>
        internal static bool IsWindowsPlatform()
        {
#if NETFRAMEWORK
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }

        /// <summary>
        /// Is this a MAC platform
        /// </summary>
        /// <returns>A value indicating if we are running on mac or not</returns>
        internal static bool IsMacPlatform()
        {
#if NETFRAMEWORK
            return Environment.OSVersion.Platform == PlatformID.MacOSX;
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif
        }

        /// <summary>
        /// Is this a linux platform
        /// </summary>
        /// <returns>A  value indicating if we are running on linux or not</returns>
        internal static bool IsLinuxPlatform()
        {
#if NETFRAMEWORK
            return Environment.OSVersion.Platform == PlatformID.Unix;
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
#endif
        }
    }
}
