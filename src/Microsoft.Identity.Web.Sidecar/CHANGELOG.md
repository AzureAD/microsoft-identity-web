# Microsoft.Identity.Web.Sidecar changelog

Changes to the `Microsoft.Identity.Web.Sidecar` container image
(`mcr.microsoft.com/entra-sdk/auth-sidecar`). Each release lists sidecar changes and the version of
Microsoft.Identity.Web it bundles.

## 1.1.1 -2026-07-17

- Microsoft.Identity.Web upgraded to [4.13.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/4.13.2).

## 1.1.0 — 2026-07-03

- Restrict sidecar endpoints to loopback callers. See [#3897](https://github.com/AzureAD/microsoft-identity-web/pull/3897).
- Suppress outbound HTTP redirects by default; opt in with `Sidecar:AllowOutboundRedirects`. See [#3906](https://github.com/AzureAD/microsoft-identity-web/pull/3906).
- Isolate downstream API options per request to prevent cross-request leakage. See [#3919](https://github.com/AzureAD/microsoft-identity-web/pull/3919).
- Add per-route override gating via `Sidecar:AllowOverrides`; `optionsOverride.BaseUrl` is always rejected. See [#3794](https://github.com/AzureAD/microsoft-identity-web/pull/3794).
- Gate `AgentIdentity`, `AgentUsername`, and `AgentUserId` query parameters behind `AllowOverrides`. See [#3871](https://github.com/AzureAD/microsoft-identity-web/pull/3871).
- Remove ASP.NET Core Data Protection configuration. See [#3776](https://github.com/AzureAD/microsoft-identity-web/pull/3776).
- Fix `optionsOverride` merging for app-token and other flows. See [#3644](https://github.com/AzureAD/microsoft-identity-web/pull/3644).
- Update AOT annotations. See [#3664](https://github.com/AzureAD/microsoft-identity-web/pull/3664).
- Upgrade the sidecar to .NET 10 (LTS). See [#3841](https://github.com/AzureAD/microsoft-identity-web/pull/3841).
- Clarify managed identity credential configuration for containers in the README. See [#3585](https://github.com/AzureAD/microsoft-identity-web/pull/3585).
- Microsoft.Identity.Web upgraded to [4.12.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/4.12.1).

## 1.0.0 — 2025-11-14

- Update the `.http` file to match the current endpoints. See [#3555](https://github.com/AzureAD/microsoft-identity-web/pull/3555).
- Target .NET 10 (Preview). See [#3449](https://github.com/AzureAD/microsoft-identity-web/pull/3449).
- Bundled Microsoft.Identity.Web: [4.0.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/4.0.1).

## 1.0.0-rc.2 — 2025-10-29

- Restrict `AllowedHosts` to `localhost` outside of Development. See [#3579](https://github.com/AzureAD/microsoft-identity-web/pull/3579).
- Bundled Microsoft.Identity.Web: [4.0.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/4.0.1).

## 1.0.0-rc.1 — 2025-10-22

- Initial release: a container for Microsoft Entra token validation and token acquisition (User OBO and application tokens for configured downstream APIs), usable from any language. See [#3524](https://github.com/AzureAD/microsoft-identity-web/pull/3524).
- Bind `ExtraHeaderParameters` and `ExtraQueryParameters` in `BindableDownstreamApiOptions`. See [#3563](https://github.com/AzureAD/microsoft-identity-web/pull/3563).
- Default `AllowWebApiToBeAuthorizedByACL` to `true`. See [#3557](https://github.com/AzureAD/microsoft-identity-web/pull/3557).
- Bundled Microsoft.Identity.Web: [4.0.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/4.0.1).
