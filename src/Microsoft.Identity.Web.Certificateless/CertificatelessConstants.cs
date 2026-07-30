// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web.Certificateless
{
    internal class CertificatelessConstants
    {
        // Managed Identity Federated Identity Credential.
        // Documented public-cloud fallback used only when MSAL's cloud metadata cannot supply the
        // audience. The primary value now comes from MSAL (single source of truth); an explicit
        // TokenExchangeUrl in configuration still overrides both.
        internal const string DefaultTokenExchangeUrl = "api://AzureADTokenExchange";

        // Well-known public-cloud authority host, used to resolve the default managed-identity token
        // exchange audience from MSAL's cloud metadata (the managed-identity leg has no AAD authority
        // of its own to key on).
        internal const string PublicCloudInstanceHost = "login.microsoftonline.com";
    }
}
