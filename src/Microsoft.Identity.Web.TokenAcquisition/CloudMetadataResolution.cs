// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Instance.Discovery;
using AbstractionsCloudKeys = Microsoft.Identity.Abstractions.CloudMetadataKeyNames;
using ICloudMetadataProvider = Microsoft.Identity.Abstractions.ICloudMetadataProvider;
using MsalCloudKeys = Microsoft.Identity.Client.Instance.Discovery.CloudMetadataKeyNames;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Resolves cloud-specific FIC (Federated Identity Credential) token-exchange values, keyed by the
    /// request's authority host, by layering (highest precedence first):
    /// <list type="number">
    /// <item>an explicit per-call override supplied by the caller;</item>
    /// <item>an upstream <see cref="ICloudMetadataProvider"/> (from DI) contributed by a caller or an
    /// upstream SDK — this is how internal-only sovereign clouds become resolvable;</item>
    /// <item>MSAL's built-in public baseline (<see cref="KnownCloudMetadata.Default"/>).</item>
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
    /// <see cref="TokenExchangeScope.FromAudience(string)"/>, the single cross-stack owner of that rule, so ID
    /// Web and MSAL can never diverge on it.
    /// </para>
    /// <para>
    /// The upstream provider and the MSAL baseline both expose the audience under the same key literal
    /// (<see cref="AbstractionsCloudKeys.FederatedCredentialAudience"/> equals
    /// <see cref="MsalCloudKeys.FederatedCredentialAudience"/>), so ID Web "translates" between the two SDKs
    /// simply by reading the same literal from whichever source resolves first — no runtime key remapping is
    /// needed.
    /// </para>
    /// </remarks>
    internal static class CloudMetadataResolution
    {
        /// <summary>
        /// Documented public-cloud default, used only when there is no authority host to key a lookup off
        /// (for example the managed-identity leg, which has no AAD authority).
        /// </summary>
        internal const string DefaultTokenExchangeAudience = "api://AzureADTokenExchange";

        /// <summary>
        /// Resolves the <b>bare</b> FIC token-exchange audience (no <c>/.default</c>) for the cloud that owns
        /// <paramref name="authorityOrInstance"/>. Suitable for managed-identity / resource contexts.
        /// </summary>
        /// <param name="authorityOrInstance">The request authority or instance (URL or bare host).</param>
        /// <param name="perCallOverride">An optional explicit override that wins over all resolved sources.</param>
        /// <param name="upstreamProvider">An optional upstream provider (from DI) contributed by a caller or
        /// an upstream SDK. When <c>null</c>, only MSAL's public baseline is consulted.</param>
        /// <exception cref="InvalidOperationException">Thrown when a non-empty authority host resolves to no
        /// cloud-specific value from any source, so ID Web cannot pick a correct audience for that cloud.</exception>
        internal static string ResolveTokenExchangeAudience(
            string? authorityOrInstance,
            string? perCallOverride,
            ICloudMetadataProvider? upstreamProvider)
        {
            // 1. Explicit per-call override always wins.
            if (!string.IsNullOrEmpty(perCallOverride))
            {
                return perCallOverride!;
            }

            string? host = TryGetHost(authorityOrInstance);

            // 2. No authority/instance to key a lookup off (e.g. the managed-identity leg, which has no AAD
            //    authority): use the documented public-cloud default rather than throwing.
            if (string.IsNullOrEmpty(host))
            {
                return DefaultTokenExchangeAudience;
            }

            // 3. Upstream provider (caller / an upstream SDK, via Abstractions) wins over MSAL's baseline. This
            //    is how internal-only sovereign clouds — which MSAL does not ship — become resolvable end-to-end.
            var upstream = upstreamProvider?.GetByAuthorityHost(host!);
            if (upstream is not null
                && upstream.TryGetValue(AbstractionsCloudKeys.FederatedCredentialAudience, out string? upstreamAudience)
                && !string.IsNullOrEmpty(upstreamAudience))
            {
                return upstreamAudience!;
            }

            // 4. MSAL public baseline. The same key literal as the upstream provider, so no remap is needed.
            var baseline = KnownCloudMetadata.Default.GetByAuthorityHost(host!);
            if (baseline is not null
                && baseline.TryGetValue(MsalCloudKeys.FederatedCredentialAudience, out string? baselineAudience)
                && !string.IsNullOrEmpty(baselineAudience))
            {
                return baselineAudience!;
            }

            // 5. A non-empty host that nothing recognized: fail loudly rather than silently exchanging against
            //    the wrong (public-cloud) audience. Sovereign or private clouds are opt-in via a provider.
            throw new InvalidOperationException(
                $"No cloud-specific federated-credential token-exchange audience is registered for authority host '{host}'. " +
                "Register it by calling services.AddCloudMetadata(...), by registering an ICloudMetadataProvider, " +
                "or set the token-exchange URL explicitly on the credential (TokenExchangeUrl).");
        }

        /// <summary>
        /// Resolves the FIC token-exchange <b>scope</b> (bare audience with <c>/.default</c> appended) for the
        /// cloud that owns <paramref name="authorityOrInstance"/>. Suitable for client-credentials / app-token
        /// contexts.
        /// </summary>
        /// <param name="authorityOrInstance">The request authority or instance (URL or bare host).</param>
        /// <param name="perCallOverride">An optional explicit override that wins over all resolved sources. It
        /// may be supplied bare or already suffixed with <c>/.default</c>; the suffix is applied idempotently.</param>
        /// <param name="upstreamProvider">An optional upstream provider (from DI) contributed by a caller or
        /// an upstream SDK. When <c>null</c>, only MSAL's public baseline is consulted.</param>
        internal static string ResolveTokenExchangeScope(
            string? authorityOrInstance,
            string? perCallOverride,
            ICloudMetadataProvider? upstreamProvider)
        {
            string audience = ResolveTokenExchangeAudience(authorityOrInstance, perCallOverride, upstreamProvider);

            // Delegate the audience→scope (/.default) computation to MSAL's TokenExchangeScope — the single
            // cross-stack owner of that rule — so ID Web and MSAL never diverge on the suffix rule.
            return TokenExchangeScope.FromAudience(audience);
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
