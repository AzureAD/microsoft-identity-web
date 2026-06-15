// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web.Diagnostics
{
    /// <summary>
    /// Exception for a failed HTTP call. This is exclusively used by reporting and never thrown.
    /// </summary>
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
