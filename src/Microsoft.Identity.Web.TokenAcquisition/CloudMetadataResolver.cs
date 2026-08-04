// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client.Instance.Discovery;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Resolves cloud-specific FIC (Federated Identity Credential) token-exchange values, keyed by the
    /// request's authority host, by layering (highest precedence first):
    /// <list type="number">
    /// <item>an explicit per-call override supplied by the caller;</item>
    /// <item>an upstream <see cref="ICloudMetadataProvider"/> (from DI) contributed by a caller or by an
    /// upstream SDK such as MISE — this is how internal-only sovereign clouds become resolvable;</item>
    /// <item>MSAL's built-in public cloud baseline (<see cref="KnownCloudConfiguration.Default"/>);</item>
    /// <item>the documented public-cloud fallback (<see cref="DefaultTokenExchangeAudience"/>).</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the single place in ID Web where the audience-vs-scope decision is made, so no call site
    /// hand-builds either form:
    /// <list type="bullet">
    /// <item>Managed-identity / "resource" contexts use the <b>bare</b> audience
    /// (<see cref="ResolveTokenExchangeAudience"/>).</item>
    /// <item>Client-credentials / app-token contexts use the computed <b>scope</b> (audience + <c>/.default</c>)
    /// (<see cref="ResolveTokenExchangeScope"/>).</item>
    /// </list>
    /// The <c>/.default</c> suffix computation itself is <b>not</b> duplicated here: it is delegated to MSAL's
    /// <see cref="CloudSettingsExtensions.TokenExchangeScope(CloudSettings)"/>, the single cross-stack owner of
    /// that rule, so ID Web and MSAL can never diverge on it.
    /// </para>
    /// <para>
    /// The upstream provider and MSAL baseline both expose the audience under the same key literal
    /// (<see cref="AbstractionsCloudKeys.TokenExchangeAudience"/> equals
    /// <c>Microsoft.Identity.Client.Instance.Discovery.MsalCloudKeys.TokenExchangeAudience</c>), so ID Web
    /// "translates" between the two SDKs simply by reading the same literal from whichever source resolves
    /// first — no runtime key remapping is needed.
    /// </para>
    /// </remarks>
    internal sealed class CloudMetadataResolver
    {
        /// <summary>
        /// Documented public-cloud fallback used only when no source resolves the requested authority host
        /// (for example an unknown/private cloud with no configured entry).
        /// </summary>
        internal const string DefaultTokenExchangeAudience = "api://AzureADTokenExchange";

        private readonly ICloudMetadataProvider? _upstreamProvider;
        private readonly ILogger? _logger;
        private readonly ConcurrentDictionary<string, byte> _warnedHosts =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudMetadataResolver"/> class.
        /// </summary>
        /// <param name="upstreamProvider">An optional upstream provider (from DI) contributed by a caller or
        /// an upstream SDK (for example MISE). When <c>null</c>, only MSAL's public baseline
        /// (<see cref="KnownCloudConfiguration.Default"/>) and the documented fallback are consulted.</param>
        /// <param name="logger">An optional logger used to emit a one-time diagnostic when a non-public
        /// authority host resolves only to the public-cloud default (a likely missing provider entry).</param>
        internal CloudMetadataResolver(ICloudMetadataProvider? upstreamProvider, ILogger? logger = null)
        {
            _upstreamProvider = upstreamProvider;
            _logger = logger;
        }

        /// <summary>
        /// Builds a resolver from the services registered in <paramref name="serviceProvider"/>: an optional
        /// upstream <see cref="ICloudMetadataProvider"/> (the single override seam) plus an optional logger.
        /// MSAL's public baseline is read directly from <see cref="KnownCloudConfiguration.Default"/>, so it
        /// is not a separate DI seam.
        /// </summary>
        /// <param name="serviceProvider">The DI service provider.</param>
        /// <returns>A configured <see cref="CloudMetadataResolver"/>.</returns>
        internal static CloudMetadataResolver FromServiceProvider(IServiceProvider serviceProvider)
        {
            return new CloudMetadataResolver(
                serviceProvider.GetService<ICloudMetadataProvider>(),
                serviceProvider.GetService<ILogger<CloudMetadataResolver>>());
        }

        /// <summary>
        /// Resolves the <b>bare</b> FIC token-exchange audience (no <c>/.default</c>) for the cloud that owns
        /// <paramref name="authorityOrInstance"/>. Suitable for managed-identity / resource contexts.
        /// </summary>
        /// <param name="authorityOrInstance">The request authority or instance (URL or bare host).</param>
        /// <param name="perCallOverride">An optional explicit override that wins over all resolved sources.</param>
        internal string ResolveTokenExchangeAudience(string? authorityOrInstance, string? perCallOverride = null)
        {
            string? resolved = ResolveAudience(authorityOrInstance, perCallOverride);
            if (resolved is not null)
            {
                return resolved;
            }

            // No source recognized the host, so fall back to the documented public-cloud default. If the
            // caller supplied a (non-public) authority host, surface a one-time warning per host: a sovereign
            // or private cloud that resolves only to the public default is almost always a missing
            // ICloudMetadataProvider entry rather than intended behavior.
            string? host = TryGetHost(authorityOrInstance);
            if (_logger != null && !string.IsNullOrEmpty(host) && _warnedHosts.TryAdd(host!, 0))
            {
                _logger.LogWarning(
                    "Microsoft.Identity.Web could not resolve cloud-specific FIC token-exchange metadata for authority host '{AuthorityHost}'; using the public-cloud default audience '{DefaultAudience}'. If this is a sovereign or private cloud, register its metadata via an ICloudMetadataProvider (for example services.AddCloudMetadata(...), or MISE's services.AddMiseCloudMetadata(...)).",
                    host,
                    DefaultTokenExchangeAudience);
            }

            return DefaultTokenExchangeAudience;
        }

        /// <summary>
        /// Resolves the FIC token-exchange <b>scope</b> (bare audience with <c>/.default</c> appended) for the
        /// cloud that owns <paramref name="authorityOrInstance"/>. Suitable for client-credentials / app-token
        /// contexts.
        /// </summary>
        /// <param name="authorityOrInstance">The request authority or instance (URL or bare host).</param>
        /// <param name="perCallOverride">An optional explicit override that wins over all resolved sources. It
        /// may be supplied bare or already suffixed with <c>/.default</c>; the suffix is applied idempotently.</param>
        internal string ResolveTokenExchangeScope(string? authorityOrInstance, string? perCallOverride = null)
        {
            string audience = ResolveTokenExchangeAudience(authorityOrInstance, perCallOverride);

            // Delegate the audience→scope (/.default) computation to MSAL's CloudSettingsExtensions — the
            // single cross-stack owner of that rule — by wrapping the resolved bare audience in a
            // CloudSettings. This guarantees ID Web and MSAL never diverge on the suffix rule.
            var settings = new CloudSettings(
                new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase)
                {
                    [MsalCloudKeys.TokenExchangeAudience] = audience,
                });

            return settings.TokenExchangeScope();
        }

        private string? ResolveAudience(string? authorityOrInstance, string? perCallOverride)
        {
            // 1. Explicit per-call override always wins.
            if (!string.IsNullOrEmpty(perCallOverride))
            {
                return perCallOverride;
            }

            string? host = TryGetHost(authorityOrInstance);

            // 2. Upstream provider (caller / MISE, via Abstractions) wins over the MSAL baseline. This is how
            //    internal-only sovereign clouds — which MSAL does not ship — become resolvable end-to-end.
            if (!string.IsNullOrEmpty(host))
            {
                CloudMetadata? upstream = _upstreamProvider?.GetByAuthorityHost(host!);
                if (upstream is not null
                    && upstream.TryGetValue(AbstractionsCloudKeys.TokenExchangeAudience, out string? upstreamAudience)
                    && !string.IsNullOrEmpty(upstreamAudience))
                {
                    return upstreamAudience;
                }
            }

            // 3. MSAL public baseline, read directly from KnownCloudConfiguration.Default (same key literal as
            //    the upstream provider — no remap needed). Reading .Default directly keeps the upstream
            //    ICloudMetadataProvider as the single override seam rather than exposing a second DI seam.
            return KnownCloudConfiguration.Default.GetSettingsByAuthorityHost(host).TokenExchangeAudience();
        }

        /// <summary>
        /// Extracts the host from an authority or instance URL (e.g.
        /// <c>https://login.microsoftonline.us/tenant</c> → <c>login.microsoftonline.us</c>). If the value
        /// is already a bare host it is returned as-is; <c>null</c>/empty yields <c>null</c>.
        /// </summary>
        internal static string? TryGetHost(string? authorityOrInstance)
        {
            if (string.IsNullOrEmpty(authorityOrInstance))
            {
                return null;
            }

            if (Uri.TryCreate(authorityOrInstance, UriKind.Absolute, out Uri? uri))
            {
                return uri.Host;
            }

            return authorityOrInstance;
        }
    }
}
