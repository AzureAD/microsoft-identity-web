// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Net;
using System.Text.Json;

// Phase-1 dev harness for the Bearer-with-bound-credential opt-in.
// Uses the MSAL SNI lab app + lab cert. Calls Key Vault as the downstream
// API. The single ClientCredential entry has UseBoundCredential=true, so
// IdWeb should route the token request to the Entra mTLS endpoint over
// mTLS instead of producing a client-assertion JWT.

var factory = TokenAcquirerFactory.GetDefaultInstance();

factory.Services.AddLogging(b => b
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information));

factory.Services.AddDownstreamApi(
    "AzureKeyVault",
    factory.Configuration.GetSection("AzureKeyVault"));

IServiceProvider sp = factory.Build();

var acquirerFactory = sp.GetRequiredService<ITokenAcquirerFactory>();
var acquirer = acquirerFactory.GetTokenAcquirer();
var tokenResult = await acquirer.GetTokenForAppAsync("https://vault.azure.net/.default");

Console.WriteLine();
Console.WriteLine("=== Bearer-with-bound-credential token result ===");
Console.WriteLine($"Expires on            : {tokenResult.ExpiresOn:u}");
Console.WriteLine($"AccessToken length    : {tokenResult.AccessToken?.Length ?? 0}");
if (!string.IsNullOrEmpty(tokenResult.AccessToken))
{
    Console.WriteLine($"AccessToken prefix    : {tokenResult.AccessToken[..Math.Min(40, tokenResult.AccessToken.Length)]}...");
}
Console.WriteLine();

IDownstreamApi api = sp.GetRequiredService<IDownstreamApi>();
HttpResponseMessage response = await api.CallApiForAppAsync("AzureKeyVault");

Console.WriteLine($"Vault status code     : {(int)response.StatusCode} {response.ReasonPhrase}");

if (response.StatusCode == HttpStatusCode.OK)
{
    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    if (doc.RootElement.TryGetProperty("value", out var valueElement) &&
        valueElement.ValueKind == JsonValueKind.String)
    {
        string secret = valueElement.GetString()!;
        Console.WriteLine($"Secret retrieved      : {(string.IsNullOrEmpty(secret) ? "EMPTY" : "OK (non-empty)")}");
    }
}
else
{
    string body = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Body                  : {body}");
}
