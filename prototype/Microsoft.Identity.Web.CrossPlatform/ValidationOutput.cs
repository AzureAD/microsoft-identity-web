// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.CrossPlatform
{

    /// <summary>
    /// Result of <see cref="Validator.ValidateAsync(MiseValidationInput, System.Threading.CancellationToken)"/> method.
    /// </summary>
    public class ValidationOutput
    {
        /// <summary>
        /// HTTP response status code.
        /// 200 means success, 401 unauthenticated (bad token or bad protocol), 403 unauthorized (the protocol
        /// was valid, but not the authorization). 
        /// </summary>
        public int HttpResponseStatusCode { get; set; }

        /// <summary>
        /// Description of the error (for humans). It will contain additional information if there was an error.
        /// To debug errors you can also enable the logs.
        /// </summary>
        public string? ErrorDescription { get; set; }

        /// <summary>
        /// WWW-Authenticate header value, if any, when HttpResponseStatusCode is 401 or 403 (you'd
        /// want to add it in the response headers of the web API using MISE).
        /// </summary>
        public string? WwwAuthenticate { get; set; }

        /// <summary>
        /// Claims in the token.
        /// </summary>
        public IDictionary<string, object>? Claims { get; set; }
    }
}
