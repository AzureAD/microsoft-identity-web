// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Microsoft.Identity.Web.CrossPlatform
{

    /// <summary>
    /// Result of Validate method.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ValidationOutputC
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="validationOutput"></param>
        public ValidationOutputC(ValidationOutput validationOutput)
        {
            _httpResponseStatusCode = validationOutput.HttpResponseStatusCode;

            if (validationOutput.ErrorDescription != null)
            {
                _errorDescription = Marshal.StringToHGlobalAnsi(validationOutput.ErrorDescription);
            }
            else
            {
                _errorDescription = IntPtr.Zero;
            }   

            if (validationOutput.WwwAuthenticate != null)
            {
                _wwwAuthenticate = Marshal.StringToHGlobalAnsi(validationOutput.WwwAuthenticate);
            }
            else
            {
                _wwwAuthenticate = IntPtr.Zero;
            }

            _claims = IntPtr.Zero;
            if (validationOutput.Claims != null)
            {
                _claimsCount = (uint)validationOutput.Claims.Count;
            }
            else
            {
                _claimsCount = 0;
            }
            
        }

        /// <summary>
        /// HTTP response status code.
        /// 200 means success, 401 unauthenticated (bad token or bad protocol), 403 unauthorized (the protocol
        /// was valid, but not the authorization). 
        /// </summary>
        private readonly int _httpResponseStatusCode;

        /// <summary>
        /// Description of the error (for humans). It will contain additional information if there was an error.
        /// To debug errors you can also enable the logs.
        /// </summary>
        private readonly IntPtr _errorDescription;

        /// <summary>
        /// WWW-Authenticate header value, if any, when HttpResponseStatusCode is 401 or 403 (you'd
        /// want to add it in the response headers of the web API using MISE).
        /// </summary>
        private readonly IntPtr _wwwAuthenticate;

        /// <summary>
        /// Claims in the token.
        /// </summary>
        private readonly IntPtr _claims;
        private readonly uint _claimsCount;
    }
}
