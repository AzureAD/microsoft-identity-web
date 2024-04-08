// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.CrossPlatform
{
    /// <summary>
    /// Input to <see cref="Validator.ValidateAsync(ValidationInput, System.Threading.CancellationToken)"/> method.
    /// </summary>
    public class ValidationInput
    {
        /// <summary>
        /// Authorization header used to call the API.
        /// </summary>
        public string? AuthorizationHeader { get; set; }
    }
}
