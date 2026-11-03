// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    internal class DefaultAuthenticationRetryPolicy : IAuthenticationRetryPolicy
    {
        public bool ShouldRetry(AuthenticationRetryContext context)
        {
            if (context.CredentialSourceLoaderParameters?.Protocol == Constants.MtlsProtocolScheme)
            {
                if (context.Response is not null &&
                    Constants.AuthFailureHttpStatusCodes.Contains(context.Response.StatusCode))
                {
                    return true;
                }
            }
            else
            {
                if (context.ThrownException is MsalServiceException msalException)
                {
                    // Only check invalid_client errors
                    if (!string.Equals(msalException.ErrorCode, Constants.InvalidClient, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    string? responseBody = msalException.ResponseBody;
                    if (responseBody is null || responseBody.Length == 0)
                    {
                        return false;
                    }

#if NET6_0_OR_GREATER
                    foreach (var errorCode in Constants.CertificateAuthFailureStsErrorCodes)
                    {
                        if (responseBody.Contains(errorCode, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
#else
                    foreach (var errorCode in Constants.CertificateAuthFailureStsErrorCodes)
                    {
                        if (responseBody.IndexOf(errorCode, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                    return false;
#endif
                }
            }

            // Unknown scenario, do not retry.
            return false;
        }
    }
}
