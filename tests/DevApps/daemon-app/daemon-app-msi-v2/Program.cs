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

factory.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

// ── 2. register the downstream API using the "AzureKeyVault" section ────
//    ProtocolScheme = "MTLS_POP" in config triggers the mTLS PoP flow
//    with managed identity (V2 credential API + attestation).
factory.Services.AddDownstreamApi("AzureKeyVault",
    factory.Configuration.GetSection("AzureKeyVault"));
IServiceProvider sp = factory.Build();
IDownstreamApi api = sp.GetRequiredService<IDownstreamApi>();

Console.WriteLine("Requesting mTLS-bound token via pure MSI (V2 credential API)...");

// ── 3. call the vault (app-token path with mTLS PoP) ────────────────────
HttpResponseMessage response = await api.CallApiForAppAsync("AzureKeyVault");

if (response.StatusCode != HttpStatusCode.OK)
{
    Console.WriteLine($"Vault returned {(int)response.StatusCode} {response.ReasonPhrase}");
    string body = await response.Content.ReadAsStringAsync();
    Console.WriteLine(body);
    return;
}

// Get the secret value from the response
using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

if (doc.RootElement.TryGetProperty("value", out var valueElement)
    && valueElement.ValueKind == JsonValueKind.String)
{
    string secret = valueElement.GetString()!;
    Console.WriteLine(!string.IsNullOrEmpty(secret)
        ? "Secret retrieved successfully with mTLS PoP token (non-null)."
        : "Secret value was empty.");
}
