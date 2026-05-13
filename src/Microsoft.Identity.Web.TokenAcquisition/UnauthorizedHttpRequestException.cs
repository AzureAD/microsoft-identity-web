// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Exception for a failed HTTP call. This is exclusively used by reporting and never thrown.
    /// </summary>
    /*
     * Used by Microsoft.Identity.Web.DownstreamApi
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
    internal class UnauthorizedHttpRequestException : Exception
    {
        public UnauthorizedHttpRequestException()
        {
        }

        public UnauthorizedHttpRequestException(string message)
            : base(message)
        {
        }

        public UnauthorizedHttpRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
