// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Net;

// OIDC FIC + mTLS Proof-of-Possession daemon sample.
//
// Same coordinates the working OidcFic_WithMtlsPop E2E test used (MSI team's
// ESTS-allowlisted-for-mtls_pop app, self-FIC pattern, LabAuth cert from CurrentUser/My).
//
// Flow:
//   1. AddOidcFic registers the loader/provider this spike adds.
//   2. The outer credential is a CustomSignedAssertion pointing at the OidcFicIdp section.
//   3. The OidcFicIdp section has two ClientCredentials: a non-bound LabAuth cert
//      (used to authenticate the inner leg to the OIDC IdP) and the same LabAuth cert
//      with UseBoundCredential = true (the spike loader resolves this as the binding
//      certificate so the outer Entra token is bound via mTLS PoP).
//   4. The downstream API call sets ProtocolScheme = "MTLS_POP" and RequestAppToken = true,
//      which triggers IdWeb's PoP path → MSAL's WithMtlsProofOfPossession().
//
// Expected output on success:
//   * Authorization header starts with "mtls_pop " (MSAL's PoP scheme name).
//   * Underlying JWT carries a "cnf" claim with x5t#S256 matching the LabAuth cert thumbprint.

var factory = TokenAcquirerFactory.GetDefaultInstance();
factory.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
factory.Services.AddOidcFic();

factory.Services.AddDownstreamApi(
    "DownstreamApi",
    factory.Configuration.GetSection("DownstreamApi"));

IServiceProvider sp = factory.Build();
IAuthorizationHeaderProvider headerProvider = sp.GetRequiredService<IAuthorizationHeaderProvider>();

string authHeader = await headerProvider.CreateAuthorizationHeaderForAppAsync(
    "https://graph.microsoft.com/.default",
    new AuthorizationHeaderProviderOptions { ProtocolScheme = "MTLS_POP" });

Console.WriteLine("Authorization header acquired:");
Console.WriteLine(authHeader.Length > 80 ? authHeader[..80] + "..." : authHeader);
Console.WriteLine();
Console.WriteLine(authHeader.StartsWith("mtls_pop", StringComparison.OrdinalIgnoreCase)
    ? "SUCCESS: Got a bound mTLS-PoP token via OIDC FIC."
    : "Got a non-PoP header (expected mtls_pop): " + authHeader.Split(' ')[0]);
