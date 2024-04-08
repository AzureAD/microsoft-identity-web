// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.C
{
    /// <summary>
    /// 
    /// </summary>
    public static class Validator
    {
        private static CrossPlatform.Validator? s_crossPlatformValidator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="microsoftIdentityApplicationOptionsPtr"></param>
        [UnmanagedCallersOnly(EntryPoint = "IdentityWebConfigure")]
        public static unsafe void Configure(IntPtr microsoftIdentityApplicationOptionsPtr)
        {
            MicrosoftIdentityApplicationOptions? msIdentityApplicationOptions = Marshal.PtrToStructure<MicrosoftIdentityApplicationOptions>(microsoftIdentityApplicationOptionsPtr);
            s_crossPlatformValidator = new(msIdentityApplicationOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validationInputPtr"></param>
        [UnmanagedCallersOnly(EntryPoint = "IdentityWebValidate")]
        public static unsafe IntPtr Validate(IntPtr validationInputPtr)
        {
            if (s_crossPlatformValidator is null)
            {
                return IntPtr.Zero;
            }

            CrossPlatform.ValidationInput? validationInput = Marshal.PtrToStructure<CrossPlatform.ValidationInput>(validationInputPtr);
            CrossPlatform.ValidationOutput? validationOutput = s_crossPlatformValidator.ValidateAsync(validationInput).Result;
            IntPtr validationOutputPtr = Marshal.AllocHGlobal(Marshal.SizeOf<CrossPlatform.ValidationOutput>());
            Marshal.StructureToPtr(validationOutput, validationOutputPtr, false);
            return validationOutputPtr;
        }
    }
}
