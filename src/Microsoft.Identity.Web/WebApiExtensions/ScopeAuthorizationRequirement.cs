// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web.Resource;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Implements an <see cref="IAuthorizationHandler"/> and <see cref="IAuthorizationRequirement"/>
    /// which requires at least one instance of the specified claim type, and, if allowed values are specified,
    /// the claim value must be any of the allowed values.
    /// </summary>
    public class ScopeAuthorizationRequirement : IAuthorizationHandler, IAuthorizationRequirement
    {
        /// <summary>
        /// Creates a new instance of <see cref="ScopeAuthorizationRequirement"/>.
        /// </summary>
        /// <param name="allowedValues">The optional list of scope values.</param>
        public ScopeAuthorizationRequirement(IEnumerable<string>? allowedValues = null)
        {
            AllowedValues = allowedValues;
        }

        /// <summary>
        /// Gets the optional list of scope values.
        /// </summary>
        public IEnumerable<string>? AllowedValues { get; }

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            // The resource is either the HttpContext or the Endpoint directly when used with the 
            // authorization middleware
            var endpoint = context.Resource switch
            {
                HttpContext httpContext => httpContext.GetEndpoint(),
                Endpoint ep => ep,
                _ => null,
            };
            var data = endpoint?.Metadata.GetMetadata<IAuthRequiredScopeMetadata>();

            var scopes = AllowedValues ?? data?.AcceptedScopes;

            // Can't determine what to do without scope metadata, so proceed
            if (scopes is null)
            {
                context.Succeed(this);
                return Task.CompletedTask;
            }

            Claim? scopeClaim = context.User.FindFirst(ClaimConstants.Scp) ?? context.User.FindFirst(ClaimConstants.Scope);

            if (scopeClaim is null)
            {
                return Task.CompletedTask;
            }

            // REVIEW: Remove the allocations here
            if (scopeClaim != null && scopeClaim.Value.Split(' ').Intersect(scopes).Any())
            {
                context.Succeed(this);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var value = (AllowedValues == null || !AllowedValues.Any())
                ? string.Empty
                : $" and `{ClaimConstants.Scp}` or `{ClaimConstants.Scope}` is one of the following values: ({string.Join("|", AllowedValues)})";

            return $"{nameof(ScopeAuthorizationRequirement)}:Scope={value}";
        }
    }
}
