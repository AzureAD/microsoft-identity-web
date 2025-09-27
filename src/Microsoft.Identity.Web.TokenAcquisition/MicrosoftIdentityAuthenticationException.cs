// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Exception thrown when authentication fails during HTTP message handling.
    /// </summary>
    public class MicrosoftIdentityAuthenticationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityAuthenticationException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public MicrosoftIdentityAuthenticationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityAuthenticationException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MicrosoftIdentityAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}