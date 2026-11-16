// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace CrossAppAccountHints;

internal sealed record AccountHint(string LoginHint, string DisplayName);

internal sealed class AccountHintSettings
{
    public string AppId { get; set; } = "A";

    public string? CookieDomain { get; set; }
}

internal sealed class AccountHintStore(AccountHintSettings settings, TimeProvider timeProvider)
{
    internal const string CookieName = "IdWeb.Poc.AccountHints";
    private const int MaxCookieLength = 3500;
    private const int MaxAccounts = 5;
    private static readonly TimeSpan s_lifetime = TimeSpan.FromHours(8);

    public string AttemptCookieName => $"IdWeb.Poc.{settings.AppId}.Attempted";

    public string SuppressionCookieName => $"IdWeb.Poc.{settings.AppId}.Suppressed";

    public AccountHint[] Read(HttpRequest request)
    {
        string? value = request.Cookies[CookieName];
        if (string.IsNullOrEmpty(value) || value.Length > MaxCookieLength)
        {
            return [];
        }

        try
        {
            HintEnvelope? envelope = JsonSerializer.Deserialize<HintEnvelope>(WebEncoders.Base64UrlDecode(value));
            if (envelope is null || envelope.Version != 1 ||
                envelope.ExpiresUtc <= timeProvider.GetUtcNow() ||
                envelope.Accounts is null || envelope.Accounts.Length > MaxAccounts ||
                envelope.Accounts.Any(account => account is null ||
                    string.IsNullOrWhiteSpace(account.LoginHint) || account.LoginHint.Length > 512 ||
                    account.LoginHint.Any(char.IsControl) ||
                    string.IsNullOrWhiteSpace(account.DisplayName) || account.DisplayName.Length > 200))
            {
                return [];
            }

            return envelope.Accounts;
        }
        catch (Exception exception) when (exception is FormatException or JsonException or ArgumentException)
        {
            return [];
        }
    }

    public void Remember(HttpContext context, ClaimsPrincipal principal)
    {
        string? loginHint = principal.FindFirst("login_hint")?.Value ??
            principal.FindFirst("preferred_username")?.Value ??
            principal.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(loginHint) || loginHint.Length > 512 || loginHint.Any(char.IsControl))
        {
            return;
        }

        string displayName = principal.FindFirst("name")?.Value ?? loginHint;
        if (displayName.Length > 200)
        {
            displayName = displayName[..200];
        }

        AccountHint[] accounts = new[] { new AccountHint(loginHint, displayName) }
            .Concat(Read(context.Request).Where(account => account.LoginHint != loginHint))
            .Take(MaxAccounts)
            .ToArray();

        string encoded;
        do
        {
            encoded = WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
                new HintEnvelope(1, timeProvider.GetUtcNow().Add(s_lifetime), accounts)));
            if (encoded.Length <= MaxCookieLength)
            {
                break;
            }

            accounts = accounts[..^1];
        }
        while (accounts.Length > 0);

        if (encoded.Length > MaxCookieLength)
        {
            return;
        }

        context.Response.Cookies.Append(CookieName, encoded, SharedCookieOptions());
    }

    public void Forget(HttpContext context)
    {
        context.Response.Cookies.Delete(CookieName, SharedCookieOptions());
    }

    public void MarkAttempt(HttpContext context)
    {
        context.Response.Cookies.Append(AttemptCookieName, "1", LocalCookieOptions());
    }

    public void Suppress(HttpContext context)
    {
        context.Response.Cookies.Append(SuppressionCookieName, "1", LocalCookieOptions());
    }

    public void ClearLocalMarkers(HttpContext context)
    {
        context.Response.Cookies.Delete(AttemptCookieName, LocalCookieOptions());
        context.Response.Cookies.Delete(SuppressionCookieName, LocalCookieOptions());
    }

    private CookieOptions SharedCookieOptions() => new()
    {
        Domain = string.IsNullOrWhiteSpace(settings.CookieDomain) ? null : settings.CookieDomain,
        Path = "/",
        Secure = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        MaxAge = s_lifetime,
        IsEssential = true,
    };

    private static CookieOptions LocalCookieOptions() => new()
    {
        Path = "/",
        Secure = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
    };

    private sealed record HintEnvelope(int Version, DateTimeOffset ExpiresUtc, AccountHint[]? Accounts);
}
