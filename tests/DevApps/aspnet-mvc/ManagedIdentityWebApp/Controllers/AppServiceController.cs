// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ManagedIdentityWebApp.Controllers;

/// <summary>
/// Acquires an App Service managed identity token via Microsoft.Identity.Web and returns the result.
/// Exercises the same IdWeb path as the TokenAcquirerTests MI E2E test, but on .NET Framework (net48)
/// and hosted in Azure App Service.
/// <para>GET /AppService?resourceuri=&lt;uri&gt;[&amp;userAssignedId=&lt;clientId&gt;]</para>
/// </summary>
public class AppServiceController : ApiController
{
    [HttpGet]
    [Route("AppService")]
    public async Task<IHttpActionResult> GetAsync(string resourceuri, string userAssignedId = null)
    {
        if (string.IsNullOrWhiteSpace(resourceuri))
        {
            return BadRequest("The 'resourceuri' query parameter is required.");
        }

        try
        {
            IAuthorizationHeaderProvider authorizationHeaderProvider =
                WebApiApplication.ServiceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            AuthorizationHeaderProviderOptions options = new()
            {
                BaseUrl = resourceuri,
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    ManagedIdentity = new ManagedIdentityOptions
                    {
                        UserAssignedClientId = userAssignedId
                    }
                }
            };

            string scope = resourceuri.TrimEnd('/') + "/.default";
            string authorizationHeader =
                await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(scope, options).ConfigureAwait(false);

            string rawToken = ExtractBearerToken(authorizationHeader);
            if (string.IsNullOrEmpty(rawToken))
            {
                // CreateAuthorizationHeaderForAppAsync normally throws on failure; guard anyway so an
                // empty header is reported as a failure the test will catch (and retry), not a success.
                return Ok("Failed to acquire managed identity token: empty authorization header.");
            }

            // Decode the token (no validation required) so CI can assert the *correct* identity was
            // used: appid must match the requested managed identity and aud must match the target
            // resource. This catches silent fallbacks, e.g. a user-assigned selector being ignored.
            JsonWebToken jwt = new JsonWebToken(rawToken);
            string appId = jwt.TryGetPayloadValue<string>("appid", out string appIdValue) ? appIdValue
                : jwt.TryGetPayloadValue<string>("azp", out string azpValue) ? azpValue
                : string.Empty;
            string audience = jwt.Audiences?.FirstOrDefault() ?? string.Empty;

            string identity = string.IsNullOrEmpty(userAssignedId)
                ? "system-assigned"
                : $"user-assigned ({userAssignedId})";

            return Ok($"Access token received. Managed identity: {identity}. appid={appId}; aud={audience}");
        }
        catch (Exception ex)
        {
            // Surface the failure in the response body so CI can see why the managed-identity call failed.
            return Ok($"Failed to acquire managed identity token: {ex}");
        }
    }

    private static string ExtractBearerToken(string authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return string.Empty;
        }

        const string bearerPrefix = "Bearer ";
        return authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader.Substring(bearerPrefix.Length).Trim()
            : authorizationHeader.Trim();
    }
}
