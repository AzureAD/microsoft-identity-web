// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web.Resource
{
    internal class RequiredScopeFilter : IAuthorizationFilter
    {
        internal readonly string[] _acceptedScopes;
        internal string[]? _effectiveAcceptedScopes;

        /// <summary>
        /// If the authenticated user does not have any of these <paramref name="acceptedScopes"/>, the
        /// method updates the HTTP response providing a status code 403 (Forbidden)
        /// and writes to the response body a message telling which scopes are expected in the token.
        /// </summary>
        /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
        /// <remarks>When the scopes don't match, the response is a 403 (Forbidden),
        /// because the user is authenticated (hence not 401), but not authorized.</remarks>
        public RequiredScopeFilter(params string[] acceptedScopes)
        {
            _acceptedScopes = acceptedScopes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_acceptedScopes == null || _acceptedScopes.Length == 0)
            {
                throw new ArgumentNullException(nameof(_acceptedScopes));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            GetEffectiveScopes(context);

            ValidateEffectiveScopes(context);
        }

        private void ValidateEffectiveScopes(AuthorizationFilterContext context)
        {
            if (_effectiveAcceptedScopes == null || !_effectiveAcceptedScopes.Any())
            {
                throw new InvalidOperationException(IDWebErrorMessage.MissingRequiredScopesForAuthorizationFilter);
            }

            if (context.HttpContext.User == null || context.HttpContext.User.Claims == null || !context.HttpContext.User.Claims.Any())
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                throw new UnauthorizedAccessException(IDWebErrorMessage.UnauthenticatedUser);
            }
            else
            {
                // Attempt with Scp claim
                Claim? scopeClaim = context.HttpContext.User.FindFirst(ClaimConstants.Scp);

                // Fallback to Scope claim name
                if (scopeClaim == null)
                {
                    scopeClaim = context.HttpContext.User.FindFirst(ClaimConstants.Scope);
                }

                if (scopeClaim == null || !scopeClaim.Value.Split(' ').Intersect(_effectiveAcceptedScopes).Any())
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    string message = string.Format(
                        CultureInfo.InvariantCulture,
                        IDWebErrorMessage.MissingScopes,
                        string.Join(",", _effectiveAcceptedScopes));
                    context.HttpContext.Response.WriteAsync(message);
                    context.HttpContext.Response.CompleteAsync();
                    throw new UnauthorizedAccessException(message);
                }
            }
        }

        private void GetEffectiveScopes(AuthorizationFilterContext context)
        {
            if (_effectiveAcceptedScopes == null)
            {
                if (_acceptedScopes.Length == 2 && _acceptedScopes[0] == Constants.RequiredScopesSetting)
                {
                    string scopeConfigurationKeyName = _acceptedScopes[1];

                    if (!string.IsNullOrWhiteSpace(scopeConfigurationKeyName))
                    {
                        // Load the injected IConfiguration
                        IConfiguration configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

                        if (configuration == null)
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    IDWebErrorMessage.ScopeKeySectionIsProvidedButNotPresentInTheServicesCollection,
                                    nameof(scopeConfigurationKeyName)));
                        }

                        _effectiveAcceptedScopes = configuration.GetValue<string>(scopeConfigurationKeyName)?.Split(' ');
                    }
                }
                else
                {
                    _effectiveAcceptedScopes = _acceptedScopes;
                }
            }
        }
    }
}
