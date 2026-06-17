// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// =============================================================================
// mTLS PoP with Pure Managed Identity (MSI)
// =============================================================================
// The binding certificate comes from the IMDS v2 credential endpoint on the
// VM/VMSS. No app-owned certificate is needed — the platform provides it.
//
// Prerequisites:
//   - Azure VM or VMSS (only platforms supporting MSI v2 credential endpoint)
//   - Managed identity assigned to the VM (system-assigned or user-assigned)
//   - Target resource must accept mTLS PoP tokens
//
// appsettings.json (user-assigned MSI):
//   {
//     "AzureKeyVault": {
//       "BaseUrl": "https://myvault.vault.azure.net/",
//       "RelativePath": "secrets/mysecret?api-version=7.4",
//       "RequestAppToken": true,
//       "ProtocolScheme": "MTLS_POP",
//       "Scopes": [ "https://vault.azure.net/.default" ],
//       "AcquireTokenOptions": {
//         "ManagedIdentity": {
//           "UserAssignedClientId": "<user-assigned-mi-client-id>"
//         }
//       },
//       "ExtraHeaderParameters": {
//         "x-ms-tokenboundauth": "true"
//       }
//     }
//   }
//
// For system-assigned MSI, use: "ManagedIdentity": { }
//
// Note: ExtraHeaderParameters with x-ms-tokenboundauth is only needed for
// Azure Key Vault. Other resources (ARM, Storage, Graph) bind the certificate
// on the initial TLS handshake and do not need this header.
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

var factory = TokenAcquirerFactory.GetDefaultInstance();

factory.Services.AddDownstreamApi(
    "AzureKeyVault",
    factory.Configuration.GetSection("AzureKeyVault"));

IServiceProvider sp = factory.Build();
IDownstreamApi api = sp.GetRequiredService<IDownstreamApi>();

// DownstreamApi handles:
//   1. Acquires mTLS PoP token from IMDS v2 (returns token + binding cert)
//   2. Creates HttpClient with binding cert via MsalMtlsHttpClientFactory
//   3. Applies ExtraHeaderParameters (x-ms-tokenboundauth: true for AKV)
//   4. Sends request with Authorization: MTLS_POP <token> + client cert via TLS
HttpResponseMessage response = await api.CallApiForAppAsync("AzureKeyVault");

Console.WriteLine($"Response: {(int)response.StatusCode} {response.ReasonPhrase}");
