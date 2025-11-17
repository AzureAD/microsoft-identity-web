# Motivation

Federated Identity Credentials provide a way to avoid managing secrets or certificates. In this case, you will rely on [Managed Identity](https://github.com/AzureAD/microsoft-identity-web/wiki/Calling-APIs-with-Managed-Identity) to issue a credential. Managed Identity abstracts away certificates from app developers.

> [!NOTE]  
> Managed Identity can issue tokens directly for your downstream APIs. However, using Managed Identity directly is limited to service principals (i.e. no user tokens) and it is single tenanted! To bypass these limitations, you can use Managed Identity to issue a federated credential. Otherwise, use Managed Identity directly!

# Setup 

This guide assumes you have setup a Federated Identity Credential with Managed Identity, as per the [Entra docs](https://learn.microsoft.com/entra/workload-id/workload-identity-federation-config-app-trust-managed-identity?tabs=microsoft-entra-admin-center )

> [!NOTE]  
> It is strongly recommended to use only User Assigned Managed Identity for issuing credentials. 

You then configure your credential in your `appsettings.json` file. Here’s a sample configuration:

```json
     {
       "AzureAd": {
         "Instance": "https://login.microsoftonline.com/",
         "TenantId": "your-tenant-id",
         "ClientId": "your-client-id",
         "ClientCredentials": [
           {
             "SourceType": "SignedAssertionFromManagedIdentity",
             "ManagedIdentityClientId": "your-user-assigned-managed-identity-client-id"
             "TokenExchangeUrl": "api://AzureADTokenExchange" // optional, it defaults api://AzureADTokenExchange, change for other clouds
           }
         ]
       }
     }
```



