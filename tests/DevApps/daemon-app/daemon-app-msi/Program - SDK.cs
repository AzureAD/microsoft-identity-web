// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Net;
using System.Text.Json;
// ── 1. bootstrap factory (reads appsettings.json automatically) ─────────
var factory = TokenAcquirerFactory.GetDefaultInstance();
// optional console logging
factory.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
// ── 2. register the downstream API using the "AzureKeyVault" section ────
factory.Services.AddDownstreamApi("AzureKeyVault",
    factory.Configuration.GetSection("AzureKeyVault"));
var sp = factory.Build();
var api = sp.GetRequiredService<IDownstreamApi>();
// ── 3. call the vault (app-token path) ──────────────────────────────────
var response = await api.CallApiForAppAsync("AzureKeyVault");
if (response.StatusCode == HttpStatusCode.OK)
{
    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    if (doc.RootElement.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.String)
    {
        string secret = valueElement.GetString()!;
        Console.WriteLine($"Secret value = {secret}");
    }
    else
    {
        Console.WriteLine("The 'value' property is missing or is not a string.");
    }
}
else
{
    Console.WriteLine($"Vault returned {(int)response.StatusCode} {response.ReasonPhrase}");
}
