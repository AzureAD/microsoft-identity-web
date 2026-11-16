# Cross-app account hints POC

An ASP.NET Core sign-in-only demo: App A remembers an account in a shared hint
cookie; App B reads that hint and starts its own Entra sign-in. A hint is never
an authentication ticket. There is no OWIN, Graph call, MSAL token cache,
`/savedusers` probe, shared authentication ticket, or client secret.

## Run and debug

Use the .NET SDK selected by the repository's `global.json` and Visual Studio
with support for the project's `net10.0` target.

From the repository root:

```powershell
dotnet dev-certs https --trust
dotnet build .\tests\DevApps\CrossAppAccountHints\CrossAppAccountHints.csproj
```

In Visual Studio, select `CrossAppAccountHints` as the startup project, select
the **AppA** launch profile, and press **F5**. After its build completes, start
App B from a terminal at the repository root:

```powershell
dotnet run --no-build --project .\tests\DevApps\CrossAppAccountHints --launch-profile AppB
```

Use **Debug > Attach to Process** to attach the managed debugger to App B if
needed. Identify the process by its command line and process ID; it runs the
same assembly as A. Do not start a second build while either instance is
running: both profiles share build outputs. Stop both instances before rebuilding.

Alternatively, after building, run these in two terminals:

```powershell
dotnet run --no-build --project .\tests\DevApps\CrossAppAccountHints --launch-profile AppA
dotnet run --no-build --project .\tests\DevApps\CrossAppAccountHints --launch-profile AppB
```

Open `https://localhost:5001` and `https://localhost:5002` in the **same browser
profile**:

1. Sign in to A.
2. Open B. It redirects to Entra with `prompt=none` and the remembered
   `login_hint`. No login UI is requested, but browser navigation still occurs.
3. If Entra can complete sign-in, B creates its own authenticated session.
   Otherwise, B returns to its anonymous page with explicit sign-in available.
4. Sign out of B. A stays signed in, and B suppresses immediate automatic sign-in.
5. Use **Choose another account** to update the shared preference. An existing
   app session is not silently switched.

Clear this demo's cookies in browser DevTools for a fresh run. The **Forget
shared hints** button removes only the shared hint; it does not sign out an app
or clear its attempt/suppression markers.

## Registration and authentication

`appsettings.json` uses the dedicated **Cross-app account hints POC** registration:

- Client ID: `7e782809-c246-49ba-8a55-a237733d7925`.
- Authority: `https://login.microsoftonline.com/common/v2.0`.
- Supported accounts: work/school accounts from any Entra tenant and personal
  Microsoft accounts.
- Web callbacks: `https://localhost:5001/signin-oidc` and
  `https://localhost:5002/signin-oidc`.
- Hybrid ID-token issuance enabled; no access-token permissions or credentials.

For this credential-free demo, `response_type=code id_token` returns an ID token
through `form_post`. The ASP.NET Core handler validates the response, and
`OnAuthorizationCodeReceived` skips code redemption. No token-endpoint call is
made. `AadIssuerValidator` handles tenant-specific issuers from the common
authority; issuer, audience, signature, nonce, and hybrid-response checks are
not disabled.

This hybrid shortcut is a POC choice, not a recommendation to replace normal
confidential-client authorization code flow. The shared-hint mechanism can also
work with code flow, with the appropriate client authentication and redemption.
Tenant policy, consent, or authentication requirements can still prevent silent
sign-in. Registration configuration does not prove real-account end-to-end success.

## Important code paths and breakpoints

| File / location | Purpose / what to inspect |
| --- | --- |
| `Program.cs`, `AddCookie` / `AddOpenIdConnect` | App-specific cookies, common authority, issuer validation, hybrid setup. |
| `Program.cs`, `OnAuthorizationCodeReceived` | Validated hybrid response; code redemption is deliberately skipped. |
| `AccountHintExtensions.cs`, `OnSignedIn` | After successful local sign-in, publish the hint and clear local markers. |
| `AccountHintStore.cs`, `Remember` | Inspect validated claims, account ordering, JSON envelope, and encoded cookie value. |
| `Program.cs`, root endpoint | Check local authentication before deciding to start automatic sign-in. |
| `AccountHintExtensions.cs`, `TryAutomaticSignInAsync` | Observe hint selection, attempt/suppression checks, and `ChallengeAsync`. |
| `AccountHintExtensions.cs`, `OnRedirectToIdentityProvider` | Inspect outgoing `prompt=none` and `login_hint`. |
| `AccountHintExtensions.cs`, `OnRemoteFailure` | Expected silent-auth errors return to the anonymous page; other failures propagate. |
| `AccountHintStore.cs`, `Read` | Decode the hint and reject malformed, expired, oversized, or invalid envelopes. |
| `AccountHintExtensions.cs`, `OnSigningOut` | Set local suppression when this app signs out. |

`AccountHintExtensions.cs` contains the reusable orchestration prototype;
`AccountHintStore.cs` contains cookie handling. `Program.cs` wires them into
vanilla ASP.NET Core and provides the demo page/actions.

## Cookies

### Shared account hint: `IdWeb.Poc.AccountHints`

The value is Base64url-encoded JSON, **not encrypted or signed**:

```json
{
  "Version": 1,
  "ExpiresUtc": "2026-11-16T23:00:00+00:00",
  "Accounts": [
    {
      "LoginHint": "opaque-login-hint-or-alice@example.test",
      "DisplayName": "Alice"
    }
  ]
}
```

- Accounts are ordered most-recent-sign-in first; the first is preferred.
- `LoginHint` comes from `login_hint`, falling back to `preferred_username`,
  then `email`. `DisplayName` comes from `name`, falling back to the login hint.
- Maximum five accounts and 3,500 encoded characters; oldest entries are dropped
  to fit. Login hints are limited to 512 characters and display names to 200.
- Eight-hour envelope expiry and cookie max-age.
- `Secure`, `HttpOnly`, `SameSite=Lax`, path `/`.
- `AccountHints:CookieDomain` is empty for localhost. For real sibling hosts it
  can specify a common parent domain, with appropriate certificates and callbacks.

The cookie contains account information, not an authoritative list of active
Entra sessions. A forged hint can trigger an authentication attempt, but cannot
establish a principal: the receiving app must validate its own OIDC response.

### Local authentication: `IdWeb.Poc.A.Auth` / `IdWeb.Poc.B.Auth`

ASP.NET Core Data Protection protects each app's authentication ticket. Each app
uses a different cookie name and Data Protection application name. Tickets have
a one-hour configured lifetime and use the handler's default sliding expiration;
this POC does not request persistent auth cookies or implement KMSI persistence.

### Local markers

- `IdWeb.Poc.A.Attempted` / `IdWeb.Poc.B.Attempted`: value `1`; prevent repeated
  automatic attempts until cleared.
- `IdWeb.Poc.A.Suppressed` / `IdWeb.Poc.B.Suppressed`: value `1`; prevent automatic
  sign-in after local sign-out.
- Both are session cookies with `Secure`, `HttpOnly`, `SameSite=Lax`, path `/`.
  Explicit sign-in/account switching clears markers; successful sign-in does too.

ASP.NET Core also creates app-specific antiforgery, correlation, and nonce cookies.
Sign-out and forgetting hints are POST actions with antiforgery validation.

Inspect cookies in **DevTools > Application > Cookies**. `HttpOnly` prevents
JavaScript reads, not inspection in DevTools or reads by a matching host's server.

## Trust boundaries and limitations

Cookies are scoped by domain and path, **not a list of trusted applications**.
Localhost cookies cross ports, so any matching localhost service can receive the
hint. A parent-domain cookie is likewise available to matching subdomains, not
just participating apps. `Secure`, `HttpOnly`, and `SameSite` do not create an
application allowlist. Signing alone would not hide account data from those hosts.

The two ports simulate hint sharing, not separate subdomains or app registrations.
Both instances reuse one client registration but maintain separate local sessions.
Production needs a reviewed privacy, integrity, account-selection, expiry, logout,
and domain-trust contract. Do not deploy this POC as a production auth solution.

## Tests

From the repository root:

```powershell
dotnet test .\tests\CrossAppAccountHints.Tests\CrossAppAccountHints.Tests.csproj --configuration Release
```

Tests use synthetic signed OIDC responses and trusted test metadata: they cover
independent sessions, shared hints, multiple tenant issuers including the consumer
tenant, issuer/tenant mismatch rejection, protocol failures, logout suppression,
account switching, invalid hints, and antiforgery. They do not prove a live
work/school or personal-account sign-in.
