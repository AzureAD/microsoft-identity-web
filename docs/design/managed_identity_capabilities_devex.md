# Using **ClientCapabilities** (`cp1`) with **Managed Identity** and **Downstream APIs**

## Why ClientCapabilities (cp1)?

Adding the `cp1` client‑capability tells Azure AD your service can handle Continuous Access Evaluation (CAE) claims challenges. Tokens will include an extra xms_cc claim, allowing near‑realtime revocation.

## Purpose

This guide shows how to declare the cp1 client capability (Continuous Access Evaluation)
in a .NET service that authenticates with Managed Identity and calls
resources through Microsoft.Identity.Web’s Downstream API helpers. You’ll learn
how to configure appsettings.json, acquire a CAE‑ready token (xms_cc=cp1),
and optionally invoke Azure Key Vault — all without secrets or certificates.

## Prerequisites

| Requirement                               | Notes                                                     |
|-------------------------------------------|-----------------------------------------------------------|
| .NET SDK                                  | **8.0** or newer                                          |
| Microsoft.Identity.Web                    | **latest**                                                |
| Microsoft.Identity.Abstractions           | **latest**                                                |
| Azure resource with Managed Identity      | VM, App Service, Functions, Container Apps, etc.          |
| Key Vault permission                      | MI needs **Get Secret** on the vault                      |

> **Tip** — For **user-assigned** MI, note the **Client ID** (GUID). Leave it blank for **system-assigned** MI.

## Specification: Configuration Layers

| Layer            | JSON path / property                                      | Examples                                                                                                  | Lifespan                 | Rationale                                                                                                  |
|------------------|-----------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|---------------------------|-------------------------------------------------------------------------------------------------------------|
| **App-level**    | `AzureAd` root → `ClientCapabilities`                     | `ClientCapabilities: [ "cp1" ]`<br>`Instance`, `TenantId`, credentials                                     | **Process-wide** — set once at startup | MSAL builds the client object **once**; everything here is stamped onto **every** token request.            |
| **Request-level**| `AcquireTokenOptions` (inside each *DownstreamApi* entry, or passed programmatically) | • `ManagedIdentity.UserAssignedClientId`<br>• `Claims` (CAE challenges)<br>• `ForceRefresh`<br>• `UseMtlsPoP` | **Per token call**        | These knobs can differ by resource or retry and therefore belong in the per-call options object.           |

### Why `cp1` stays at the App-level
* **Identity of the client** – signals a *capability of the app*, not an individual request.  

## Configuration 

```
{
  "AzureAd": {
    "ClientCapabilities": [ "cp1" ]
  },

  "AzureKeyVault": {
    "BaseUrl": "https://<your‑vault>.vault.azure.net/",
    "RelativePath": "secrets/<secret-name>?api-version=7.4",
    "RequestAppToken": true,
    "Scopes": [ "https://vault.azure.net/.default" ],

    "AcquireTokenOptions": {
      "ManagedIdentity": {
        "UserAssignedClientId": "<user-assigned-mi-client-id>"
      }
    }
  }
}
```

## Code 

```cs
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
```

## Options as seen in MSAL 

![alt text](capab1.png)