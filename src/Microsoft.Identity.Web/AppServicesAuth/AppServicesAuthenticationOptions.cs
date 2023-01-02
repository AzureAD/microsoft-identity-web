// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options for Azure App Services authentication.
    /// </summary>
    public class AppServicesAuthenticationOptions : AuthenticationSchemeOptions
    {
        private string _roleClaimType = ClaimsIdentity.DefaultRoleClaimType;

        /// <summary>
        /// Gets or sets the <see cref="string"/> that defines the <see cref="ClaimsIdentity.RoleClaimType"/>.
        /// </summary>
        /// <remarks>
        /// <para>Controls the results of <see cref="ClaimsPrincipal.IsInRole( string )"/>.</para>
        /// <para>Each <see cref="Claim"/> where <see cref="Claim.Type"/> == <see cref="RoleClaimType"/> will be checked for a match against the 'string' passed to <see cref="ClaimsPrincipal.IsInRole(string)"/>.</para>
        /// The default is <see cref="ClaimsIdentity.DefaultRoleClaimType"/>.
        /// </remarks>
        public string RoleClaimType
        {
            get
            {
                return _roleClaimType;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw LogHelper.LogExceptionMessage(new ArgumentOutOfRangeException(nameof(value), "RoleClaimType cannot be null or whitespace."));

                _roleClaimType = value;
            }
        }
    }
}
