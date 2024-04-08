// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.CrossPlatform
{
    /// <summary>
    /// Input to <see cref="Validator.ValidateAsync(ValidationInputC, System.Threading.CancellationToken)"/> method.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ValidationInputC
    {
        /// <summary>
        /// Authorization header used to call the API.
        /// </summary>
        private readonly IntPtr _authorizationHeader;

        /// <summary>
        /// 
        /// </summary>
        public readonly string? AuthorizationHeader => Marshal.PtrToStringUTF8(_authorizationHeader);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ValidationInput ToValidationInput()
        {
            return new ValidationInput()
            {
                AuthorizationHeader = AuthorizationHeader,
            };
        }
    }
}
