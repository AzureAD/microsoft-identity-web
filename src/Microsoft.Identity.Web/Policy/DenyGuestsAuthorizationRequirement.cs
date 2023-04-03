// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implements an <see cref="IAuthorizationRequirement"/>
    /// which requires the current user to be a member of the tenant.
    /// </summary>
    public class DenyGuestsAuthorizationRequirement : IAuthorizationRequirement
    {
    }
}
