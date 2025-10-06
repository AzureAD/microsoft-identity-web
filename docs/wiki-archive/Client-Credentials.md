# What are client credentials

Credentials enable confidential applications to identify themselves to the authentication service when receiving tokens. These are configurable in the "Certificates & Secrets" section of the Entra Application Registration in the Azure Portal.

These are not the same as user credentials (i.e. user passwords), which are known to users.

Entra supports [3 types of credentials](https://learn.microsoft.com/entra/identity-platform/how-to-add-credentials?tabs=certificate)

- secrets
- certificates
- federated credentials 

| Credential Type | What Is It | When to Use | Advantages | Considerations |
|----------------|------------|-------------|------------|----------------|
| **Secret** <br>  | Simple shared secret string | • Development/testing<br>• Basic security requirements | • Simple to use<br>• Easy to configure | **Not for production:**<br>• Less secure<br>• No auto-rotation<br>• Easy to expose |
| **Certificate** <br> | Certificate in Windows Certificate Store | Applications not hosted on Azure | • More secure than secrets • Only the public key is exposed | Certificate rotation can be cumbersome |
| **Federated Credentials** <br> | Credentials issued by another provider | For federation with other Identity Providers (e.g. GitHub) or federation with Azure Managed Identity | • Eliminates the need to an extra credential <br>• When federating with Managed Identity, 0 credential setup | Ideal for apps hosted on Azure |

The preferred credential to use in production is [Federated Credential with Managed Identity](https://github.com/AzureAD/microsoft-identity-web/wiki/Federated-Identity-Credential-(FIC)-with-a-Managed-Service-Identity-(MSI)). 

