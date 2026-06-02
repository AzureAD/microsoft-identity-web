// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Net;

// Federated Identity Credentials (FIC) backed by Managed Identity, exchanged for an
// app token via mTLS Proof-of-Possession.
//
// Two-leg flow:
//   Leg 1 — Managed Identity mints a binding cert + signed assertion (audience: AzureADTokenExchange).
//   Leg 2 — Entra ID exchanges that assertion for a resource access token, pinning it to the same cert.
//
// Trigger: AuthorizationHeaderProviderOptions.ProtocolScheme = "MTLS_POP" (in appsettings.json
// under AcquireTokenOptions, with RequestAppToken = true) plus ClientCredentials of type
// SignedAssertionFromManagedIdentity.

var factory = TokenAcquirerFactory.GetDefaultInstance();

factory.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

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

Console.WriteLine("Secret retrieved successfully via FIC + mTLS PoP.");
