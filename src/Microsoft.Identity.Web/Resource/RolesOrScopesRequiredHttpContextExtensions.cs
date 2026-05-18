// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using global::Microsoft.AspNetCore.Http;
using global::Microsoft.Identity.Web;
using System.Globalization;
using System.Net;
using System.Security.Claims;

namespace Microsoft.Identity.Web.Resource;

/// <summary>
/// Extension class providing the extension methods for <see cref="HttpContent"/> that
/// can be used in web APIs to validate scopes and roles in controller actions.
/// </summary>
public static class RolesOrScopesRequiredHttpContextExtensions
{
    /// <summary>
    /// When applied to an <see cref="HttpContext"/>, verifies that the user authenticated in the
    /// web API has any of the accepted scopes or roles.
    /// If there is no authenticated user, the response is a 401 (Unauthenticated).
    /// If the authenticated user does not have any of these <paramref name="acceptedScopes"/> or <paramref name="acceptedRoles"/>, the
    /// method updates the HTTP response providing a status code 403 (Forbidden)
    /// and writes to the response body a message telling which scopes or roles are expected in the token.
    /// </summary>
    /// <param name="context">HttpContext (from the controller).</param>
    /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
    /// <param name="acceptedRoles">Roles accepted by this web API.</param>

    public static void ValidateAppRolesOrScopes(this HttpContext context, string[] acceptedScopes, string[] acceptedRoles)
    {
        if (acceptedScopes?.Any() != true && acceptedRoles?.Any() != true)
        {
            throw new ArgumentException($"{nameof(acceptedScopes)} and {nameof(acceptedRoles)} are null or empty");
        }
        ArgumentNullException.ThrowIfNull(context);

        IEnumerable<Claim> userClaims;
        ClaimsPrincipal user;

        // Need to lock due to https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
        lock (context)
        {
            user = context.User;
            userClaims = user.Claims;
        }

        if (user == null || userClaims == null || !userClaims.Any())
        {
            lock (context)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            //throw new UnauthorizedAccessException(IDWebErrorMessage.UnauthenticatedUser); 
            throw new UnauthorizedAccessException("IDW10204: The user is unauthenticated. The HttpContext does not contain any claims. ");
        }

        var hasScopeOrRole = false;
        if (acceptedScopes?.Any() == true)
        {
            var scpClaim = user.FindFirst(ClaimConstants.Scp)?.Value?.Split(' ');
            var scopeClaim = user.FindFirst(ClaimConstants.Scope)?.Value?.Split(' ');

            hasScopeOrRole = scpClaim?.Any(acceptedScopes.Contains) == true || scopeClaim?.Any(acceptedScopes.Contains) == true;
        }
        if (acceptedRoles?.Any() == true)
        {
            var rolesClaim = userClaims.Where(claims => claims.Type == ClaimConstants.Roles || claims.Type == ClaimConstants.Role)
                    .SelectMany(roles => roles.Value.Split(' '));
            hasScopeOrRole = rolesClaim?.Any(acceptedRoles.Contains) == true;
        }
        if (hasScopeOrRole)
        {
            return;
        }

        var message = string.Format(CultureInfo.InvariantCulture,
                        $"The 'scope' or 'scp' claim does not contain scopes '{0}' nor " +
                        $"the 'roles' or 'role' claim does not contain roles '{1}'",
                        new string[] { string.Join(",", acceptedScopes), string.Join(",", acceptedRoles) });

        lock (context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.WriteAsync(message);
            context.Response.CompleteAsync();
        }
        throw new UnauthorizedAccessException(message);
    }
}
