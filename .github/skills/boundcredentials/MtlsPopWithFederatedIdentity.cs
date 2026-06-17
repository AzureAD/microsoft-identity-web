// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// =============================================================================
// mTLS PoP with Federated Identity Credential (FIC)
// =============================================================================
// Uses a managed identity's signed assertion as a client credential for a
// regular Entra ID app registration. The app authenticates as itself (confidential
// client) but proves identity using the MSI assertion instead of a secret/cert.
//
// Prerequisites:
//   - Azure VM or VMSS with user-assigned managed identity
//   - Entra ID app registration with federated credential configured
//     (federated credential issuer = managed identity)
//   - Target resource must accept mTLS PoP tokens
//
// appsettings.json:
//   {
//     "AzureAd": {
//       "Instance": "https://login.microsoftonline.com/",
//       "TenantId": "<tenant-id>",
//       "ClientId": "<app-client-id>",
//       "ClientCredentials": [
//         {
//           "SourceType": "SignedAssertionFromManagedIdentity",
//           "ManagedIdentityClientId": "<user-assigned-mi-client-id>"
//         }
//       ]
//     },
//     "AzureKeyVault": {
//       "BaseUrl": "https://myvault.vault.azure.net/",
//       "RelativePath": "secrets/mysecret?api-version=7.4",
//       "RequestAppToken": true,
//       "ProtocolScheme": "MTLS_POP",
//       "Scopes": [ "https://vault.azure.net/.default" ],
//       "ExtraHeaderParameters": {
//         "x-ms-tokenboundauth": "true"
//       }
//     }
//   }
//
// Note: The FIC credential (SignedAssertionFromManagedIdentity) is configured
// under AzureAd.ClientCredentials. The downstream API config is the same shape
// as other credential types — ExtraHeaderParameters works identically regardless
// of how the token was acquired.
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
//   1. Acquires token using FIC (MSI signed assertion as client credential)
//   2. MSAL returns mTLS PoP token bound to the cert
//   3. Creates HttpClient with cert via MsalMtlsHttpClientFactory
//   4. Applies ExtraHeaderParameters (x-ms-tokenboundauth: true for AKV)
//   5. Sends request with Authorization: MTLS_POP <token> + client cert via TLS
HttpResponseMessage response = await api.CallApiForAppAsync("AzureKeyVault");

Console.WriteLine($"Response: {(int)response.StatusCode} {response.ReasonPhrase}");
