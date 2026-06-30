// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;

namespace Microsoft.Identity.Web.Sidecar;

/// <summary>
/// Restricts the sidecar to callers connecting over the loopback interface
/// (the co-located application), responding with 403 to other callers. The
/// health endpoint is exempt so liveness/readiness probes, which target the
/// pod's routable address, continue to work.
/// </summary>
public static class LocalCallerRestriction
{
    /// <summary>
    /// Path of the health endpoint. It remains reachable from non-loopback
    /// callers (for example, orchestrator liveness/readiness probes).
    /// </summary>
    public const string HealthEndpointPath = "/healthz";

    private static readonly PathString s_healthEndpoint = new(HealthEndpointPath);

    /// <summary>
    /// Adds middleware that rejects, with <c>403 Forbidden</c>, any request
    /// whose connection does not originate from the loopback interface, except
    /// requests to the health endpoint.
    /// </summary>
    /// <param name="app">The application to configure.</param>
    public static void UseLocalCallerRestriction(this WebApplication app)
    {
        app.Use(static (context, next) =>
        {
            if (IsLocal(context.Connection.RemoteIpAddress) ||
                context.Request.Path.Equals(s_healthEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                return next(context);
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        });
    }

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
