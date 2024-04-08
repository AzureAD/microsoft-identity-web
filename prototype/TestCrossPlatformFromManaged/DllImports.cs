// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace TestCrossPlatformFromManaged
{
    internal static class DllImports
    {
        [DllImport("Microsoft.Identity.Web.C.dll", EntryPoint = "IdentityWebConfigure")]
        public static extern IntPtr IdentityWebConfigure(IntPtr microsoftIdentityApplicationOptionsPtr);

        [DllImport("Microsoft.Identity.Web.C.dll", EntryPoint = "IdentityWebValidate")]
        public static extern IntPtr Validate(IntPtr validationInputPtr);
    }
}
