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
IServiceProvider sp = factory.Build();
IDownstreamApi api = sp.GetRequiredService<IDownstreamApi>();

// ── 3. call the vault (app-token path) ──────────────────────────────────
HttpResponseMessage response = await api.CallApiForAppAsync("AzureKeyVault");

if (response.StatusCode != HttpStatusCode.OK)
{
    Console.WriteLine($"Vault returned {(int)response.StatusCode} {response.ReasonPhrase}");
    return;
}

//Get the secret value from the response
using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

// Check if the "value" property exists and is a string
if (doc.RootElement.TryGetProperty("value", out var valueElement) && valueElement.ValueKind == JsonValueKind.String)
{
    // Retrieve the secret value but do not print it
    string secret = valueElement.GetString()!;

    // Optionally, you can check if the secret is not null or empty
    if (!string.IsNullOrEmpty(secret))
    {
        Console.WriteLine("Secret retrieved successfully (non-null).");
    }
    else
    {
        Console.WriteLine("Secret value was empty.");
    }
}
