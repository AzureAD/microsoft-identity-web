## Motivation

Federated Identity Credentials provide a way to avoid managing secrets or certificate credentials. In this case, the credential is issued by another OIDC compliant Identity Provider (IdP). The federated identity credential creates a trust relationship between an application and an external IdP. 

This avoids having to manage an extra credential for Entra. You still have to manage a credential in the external IdP.

## Setup

See [the Entra docs](https://learn.microsoft.com/entra/workload-id/workload-identity-federation-create-trust?pivots=identity-wif-apps-methods-azp) for how to set this up.

## Config

1. In the appsettings.json that Microsoft.Identity.Web uses, you declare a separate section in your config for the external IdP.

```json
{
    "$schema": "https://raw.githubusercontent.com/AzureAD/microsoft-identity-web/refs/heads/master/JsonSchemas/microsoft-identity-web.json",
    "AzureAD": {
        "Instance": "https://login.microsoftonline.com/",
        "TenantId": "Entra_tenent_id",
        "ClientId": "Entra_client_id", 
        "ClientCredentials": [  
            {
                "SourceType": "CustomSignedAssertion",
                "CustomSignedAssertionProviderName": "OidcIdpSignedAssertion",
                "CustomSignedAssertionProviderData": {
                    "ConfigurationSection": "CredentialSection" // reference to the section below
                }
            }
        ]
    },

    "CredentialSection": {
        "Instance":    "https://login.microsoftonline.com/"           // Use Instance + TenantID for Entra and "Authority" for other Identity Providers
        "TenantId": "Entra_tenent_id"
        "ClientId": "external_idp_client_id",
        "ClientCredentials": [  // the external IdP still needs a credential
            {
                "SourceType": "StoreWithDistinguishedName",
                "CertificateStorePath": "CurrentUser/My",
                "CertificateDistinguishedName": "CN=my_cert_cn"
            }
        ]
    }
}

```
2. Add a reference to Microsoft.Identity.Web.OidcFIC
3. Inject the new credential 

```diff
 TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
 TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
+ tokenAcquirerFactory.Services.AddOidcFic();
```

4. Get the authroization header or call downstream APIs as usual

## Further reading

For a setup that uses Entra-to-Entra, see [this integration test](https://github.com/AzureAD/microsoft-identity-web/pull/3255/files#diff-1675fd839fa070fb85bfb2cd31afbcc07c142b102c7e023d82347e6313dbd184). 

For a code-only setup, see [this test](https://github.com/AzureAD/microsoft-identity-web/pull/3255/files#diff-4de910abfb03ddf321b20a57203495804316b6badf27a3d8d0e3b1f15509f96eR82).

