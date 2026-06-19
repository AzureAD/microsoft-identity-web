// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// =============================================================================
// mTLS PoP with Certificate Credential
// =============================================================================
// The app uses its own certificate for both client authentication and PoP
// token binding. The certificate can come from Key Vault, local store, or file.
//
// Prerequisites:
//   - Entra ID app registration with the certificate uploaded
//   - Certificate accessible at runtime (Key Vault, cert store, or PFX file)
//
// appsettings.json:
//   {
//     "AzureAd": {
//       "Instance": "https://login.microsoftonline.com/",
//       "TenantId": "<tenant-id>",
//       "ClientId": "<client-id>",
//       "ClientCredentials": [
//         {
//           "SourceType": "KeyVault",
//           "KeyVaultUrl": "https://myvault.vault.azure.net",
//           "KeyVaultCertificateName": "my-app-cert"
//         }
//       ]
//     },
//     "DownstreamApi": {
//       "BaseUrl": "https://myapi.contoso.com/",
//       "RequestAppToken": true,
//       "ProtocolScheme": "MTLS_POP",
//       "Scopes": [ "api://<api-client-id>/.default" ]
//     }
//   }
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

var factory = TokenAcquirerFactory.GetDefaultInstance();

factory.Services.AddDownstreamApi(
    "DownstreamApi",
    factory.Configuration.GetSection("DownstreamApi"));

IServiceProvider sp = factory.Build();
IDownstreamApi api = sp.GetRequiredService<IDownstreamApi>();

// DownstreamApi handles:
//   1. Acquires mTLS PoP token using the app's certificate
//   2. Creates HttpClient with the cert attached for mutual TLS
//   3. Sends request with Authorization: MTLS_POP <token>
HttpResponseMessage response = await api.CallApiForAppAsync("DownstreamApi");

Console.WriteLine($"Response: {(int)response.StatusCode} {response.ReasonPhrase}");
