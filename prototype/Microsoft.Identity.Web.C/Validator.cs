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
        public static unsafe void Configure(MicrosoftIdentityApplicationOptionsC* microsoftIdentityApplicationOptionsPtr)
        {
            Console.WriteLine(microsoftIdentityApplicationOptionsPtr->Authority);
            Console.WriteLine(microsoftIdentityApplicationOptionsPtr->Audience);

            s_crossPlatformValidator = new CrossPlatform.Validator(new MicrosoftIdentityApplicationOptions
            {
                Authority = microsoftIdentityApplicationOptionsPtr->Authority,
                Audience = microsoftIdentityApplicationOptionsPtr->Audience,
                Audiences = microsoftIdentityApplicationOptionsPtr->Audiences,
            });
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

            CrossPlatform.ValidationInputC? validationInput = Marshal.PtrToStructure<CrossPlatform.ValidationInputC>(validationInputPtr);
            Console.WriteLine(validationInput.Value.AuthorizationHeader);
            var result = s_crossPlatformValidator.ValidateAsync(validationInput.Value.ToValidationInput()).Result;
            if (result == null)
            {
                Console.WriteLine("result is null");
                return IntPtr.Zero;
            }
            else
            {
                Console.WriteLine(result.HttpResponseStatusCode);
                CrossPlatform.ValidationOutputC? validationOutput = new CrossPlatform.ValidationOutputC(result);
                IntPtr validationOutputPtr = Marshal.AllocHGlobal(Marshal.SizeOf<CrossPlatform.ValidationOutputC>());
                Marshal.StructureToPtr(validationOutput, validationOutputPtr, false);
                return validationOutputPtr;
            }
        }
    }
}
