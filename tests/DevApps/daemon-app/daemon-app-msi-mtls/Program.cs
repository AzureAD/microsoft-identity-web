// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Net;

// Pure Managed Identity with mTLS Proof-of-Possession.
// Requires a supported Windows IMDSv2 host with Credential Guard/KeyGuard for attestation.
//
// Select ProtocolScheme = "MTLS_POP" with RequestAppToken = true in appsettings.json.
// AcquireTokenOptions.MinimumTokenBindingStrength explicitly requires KeyGuard in this sample.
//
// Important: Azure Key Vault requires "x-ms-tokenboundauth": "true" header on mTLS PoP
// requests to trigger TLS renegotiation for client cert binding. This is configured via
// ExtraHeaderParameters in appsettings.json. Without it, AKV returns 401.

var factory = TokenAcquirerFactory.GetDefaultInstance();

factory.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
factory.Services.AddMicrosoftIdentityWebKeyAttestation();

factory.Services.AddDownstreamApi(
    "AzureKeyVault",
    factory.Configuration.GetSection("AzureKeyVault"));

IServiceProvider sp = factory.Build();
IDownstreamApi api = sp.GetRequiredService<IDownstreamApi>();

HttpResponseMessage response = await api.CallApiForAppAsync("AzureKeyVault");

if (response.StatusCode != HttpStatusCode.OK)
{
    Console.WriteLine($"Vault returned {(int)response.StatusCode} {response.ReasonPhrase}");
    return;
}

Console.WriteLine("Secret retrieved successfully via MSI mTLS PoP.");
