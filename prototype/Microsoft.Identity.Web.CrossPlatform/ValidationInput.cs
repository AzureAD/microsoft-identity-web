// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.CrossPlatform
{
    /// <summary>
    /// Input to Validate method.
    /// </summary>
    public class ValidationInput
    {
        /// <summary>
        /// Authorization header used to call the API.
        /// </summary>
        public string? AuthorizationHeader { get; set; }
    }
}
