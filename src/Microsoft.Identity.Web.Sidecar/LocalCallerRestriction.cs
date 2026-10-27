// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Identity.Web.Sidecar;

/// <summary>
/// Restricts the sidecar to its co-located application. Outside Development a
/// request must originate from the loopback interface (otherwise
/// <c>403 Forbidden</c>) and carry a local host name in its <c>Host</c> header
/// (otherwise <c>400 Bad Request</c>). The health endpoint is exempt from both
/// checks so liveness/readiness probes, which target the pod's routable
/// address, continue to work.
/// </summary>
public static class LocalCallerRestriction
{
    /// <summary>
    /// Path of the health endpoint. It remains reachable from non-loopback
    /// callers (for example, orchestrator liveness/readiness probes).
    /// </summary>
    public const string HealthEndpointPath = "/healthz";

    private static readonly PathString s_healthEndpoint = new(HealthEndpointPath);
    private static readonly PathString s_healthEndpointWithSlash = new(HealthEndpointPath + "/");

    // Local host names accepted in the Host header. HostString.MatchesAny
    // preserves the framework's port, casing, and IPv6 matching behavior.
    private static readonly StringSegment[] s_allowedHosts =
        new StringSegment[] { "localhost", "127.0.0.1", "[::1]" };

    /// <summary>
    /// Adds middleware that, for every request except the health endpoint,
    /// rejects callers that do not connect over the loopback interface with
    /// <c>403 Forbidden</c>, and requests whose <c>Host</c> header is not a
    /// local host name with <c>400 Bad Request</c>.
    /// </summary>
    /// <param name="app">The application to configure.</param>
    public static void UseLocalCallerRestriction(this WebApplication app)
    {
        app.Use(static (context, next) =>
        {
            if (IsHealthEndpoint(context.Request.Path))
            {
                return next(context);
            }

            if (!IsLocal(context.Connection.RemoteIpAddress))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            if (!HasLocalHost(context.Request.Host))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Task.CompletedTask;
            }

            return next(context);
        });
    }

    // Exempts the health endpoint, including the trailing-slash form that
    // routing also accepts, but not arbitrary subpaths beneath it.
    private static bool IsHealthEndpoint(PathString path) =>
        path.Equals(s_healthEndpoint, StringComparison.OrdinalIgnoreCase) ||
        path.Equals(s_healthEndpointWithSlash, StringComparison.OrdinalIgnoreCase);

    // An absent Host header is allowed (parity with the framework's
    // HostFilteringOptions.AllowEmptyHosts default); the loopback check above
    // already gates access.
    private static bool HasLocalHost(HostString host) =>
        string.IsNullOrEmpty(host.Value) ||
        HostString.MatchesAny(host.Value, s_allowedHosts);

    /// <summary>
    /// Determines whether a connection originates from the local host.
    /// A <see langword="null"/> address (for example, in-process hosting that
    /// has no transport peer) is treated as local.
    /// </summary>
    internal static bool IsLocal(IPAddress? remoteIpAddress)
    {
        if (remoteIpAddress is null)
        {
            // Allow a null address for the local IPC transport (Unix socket /
            // named pipe) use case; never null over TCP, so remote callers stay blocked.
            return true;
        }

        if (IPAddress.IsLoopback(remoteIpAddress))
        {
            return true;
        }

        // A loopback connection can be surfaced in its IPv4-mapped IPv6 form
        // (for example, ::ffff:127.0.0.1); normalize before re-checking.
        return remoteIpAddress.IsIPv4MappedToIPv6 &&
               IPAddress.IsLoopback(remoteIpAddress.MapToIPv4());
    }
}
