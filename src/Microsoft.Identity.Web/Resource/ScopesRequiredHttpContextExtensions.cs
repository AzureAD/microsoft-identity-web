// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// This extension class is now Obsolete.
    /// </summary>
    [Obsolete(IDWebErrorMessage.VerifyUserHasAnyAcceptedScopeIsObsolete, true)]
    public static class ScopesRequiredHttpContextExtensions
    {
        /// <summary>
        /// This method is now Obsolete.
        /// </summary>
        /// <param name="context">HttpContext (from the controller).</param>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        [Obsolete(IDWebErrorMessage.VerifyUserHasAnyAcceptedScopeIsObsolete, true)]
        public static void VerifyUserHasAnyAcceptedScope(this HttpContext context, params string[] acceptedScopes)
        {
            throw new NotImplementedException(IDWebErrorMessage.AadIssuerValidatorGetIssuerValidatorIsObsolete);
        }
    }
}
