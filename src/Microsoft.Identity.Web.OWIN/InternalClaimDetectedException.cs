// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Claims;

namespace Microsoft.Identity.Web.OWIN
{
    /// <summary>
    /// The exception that is thrown when an internal ID Token claim used by Microsoft.Identity.Web internally is detected in the user's ID Token.
    /// </summary>
    public class InternalClaimDetectedException : Exception
    {
        /// <summary>
        /// Gets or sets the invalid claim.
        /// </summary>
        public Claim Claim { get; set; }

        public InternalClaimDetectedException()
        {
        }

        public InternalClaimDetectedException(string message) : base(message)
        {
        }

        public InternalClaimDetectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
