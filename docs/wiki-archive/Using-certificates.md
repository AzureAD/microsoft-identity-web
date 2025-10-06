## Using certificates with Microsoft.Identity.Web

Microsoft.Identity.Web uses certificates in two situations:

- In web apps and web APIs, to prove the identity of the application, instead of using a client secret.
- In web APIs, to decrypt tokens if the web API opted to get encrypted tokens.

This article explains both usages, as well as describes the certificates to use.

Table of contents:
- [Using certificates with Microsoft.Identity.Web](#using-certificates-with-microsoftidentityweb)
  - [Client certificates](#client-certificates)
    - [Describing client certificates to use by configuration](#describing-client-certificates-to-use-by-configuration)
    - [Describing client certificates to use programmatically](#describing-client-certificates-to-use-programmatically)
  - [Decryption certificates](#decryption-certificates)
    - [Describing decryption certificates to use by configuration](#describing-decryption-certificates-to-use-by-configuration)
    - [Describing decryption certificates to use programmatically](#describing-decryption-certificates-to-use-programmatically)
    - [Helping certificate rotation by sending x5c](#helping-certificate-rotation-by-sending-x5c)
  - [Specifying certificates](#specifying-certificates)
    - [Getting certificates from Key Vault](#getting-certificates-from-key-vault)
      - [Specifying client certificate from Key Vault by configuration](#specifying-client-certificate-from-key-vault-by-configuration)
      - [Specifying client certificate from Key Vault programmatically](#specifying-client-certificate-from-key-vault-programmatically)
    - [Specifying certificates from a path](#specifying-certificates-from-a-path)
      - [Specifying certificates from a path by configuration](#specifying-certificates-from-a-path-by-configuration)
      - [Specifying certificates from a path programmatically](#specifying-certificates-from-a-path-programmatically)
    - [Specifying certificates from a certificate store by distinguished name](#specifying-certificates-from-a-certificate-store-by-distinguished-name)
      - [Specifying certificates from a certificate store by distinguished name by configuration](#specifying-certificates-from-a-certificate-store-by-distinguished-name-by-configuration)
      - [Specifying certificates from a certificate store by distinguished name programmatically](#specifying-certificates-from-a-certificate-store-by-distinguished-name-programmatically)
    - [Specifying certificates from a certificate store by thumbprint](#specifying-certificates-from-a-certificate-store-by-thumbprint)
      - [Specifying certificates from a certificate store by thumbprint by configuration](#specifying-certificates-from-a-certificate-store-by-thumbprint-by-configuration)
      - [Specifying certificates from a certificate store by thumbprint programmatically](#specifying-certificates-from-a-certificate-store-by-thumbprint-programmatically)
    - [Specifying certificates from a certificate store by Base64 encoded value](#specifying-certificates-from-a-certificate-store-by-base64-encoded-value)
      - [Specifying certificates from a certificate store by Base64 encoded value by configuration](#specifying-certificates-from-a-certificate-store-by-base64-encoded-value-by-configuration)
      - [Specifying certificates from a certificate store by Base64 encoded value programmatically](#specifying-certificates-from-a-certificate-store-by-base64-encoded-value-programmatically)
    - [Specifying certificates as an X509Certificate2](#specifying-certificates-as-an-x509certificate2)
  - [Microsoft Identity Web classes used for certificate management](#microsoft-identity-web-classes-used-for-certificate-management)

### Client certificates

Web apps and web APIs are confidential client applications.

They can prove their identity to Azure AD or Azure AD B2C by 3 means:

| Method              | Supported in Microsoft.Identity.Web |
| ------------------- | ----------------------------------- |
| Client secrets      | Yes                                 |
| Client certificates | Yes                                 |
| Client assertions   | Not yet                             |

 Microsoft.Identity.Web supports specifying client certificates. The configuration property to specify the client certificates is **ClientCertificates**. It is an array of certificate descriptions. There are several ways of describing certificates. see [Specifying certificates](#specifying-certificates) below.

#### Describing client certificates to use by configuration

You can express the client certificates in the **ClientCertificates** property. **ClientCertificates** and **ClientSecret** are mutually exclusive.

```Json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "msidentitysamplestesting.onmicrosoft.com",
    "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
    "ClientId": "86699d80-dd21-476a-bcd1-7c1a3d471f75",

    "ClientCertificates": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://msidentitywebsamples.vault.azure.net",
        "KeyVaultCertificateName": "MicrosoftIdentitySamplesCert"
      }
     ]
  }
}
```

See [Specifying certificates](#specifying-certificates) below for all the ways to describe certificates.

#### Describing client certificates to use programmatically

You can also specify the certificate description programmatically. For this you add `CertificateDescription` instances to the `ClientCertificates` property of `MicrosoftIdentityOptions`. You can then use some of the overloads of `AddMicrosoftIdentityWebApp`, `EnableTokenAcquisitionToCallDownstreamApi` using delegates to set the `MicrosoftIdentityOptions`.

```Csharp
MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
options.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromKeyVault("https://msidentitywebsamples.vault.azure.net",
                                     "MicrosoftIdentitySamplesCert")
};
```

See [Specifying certificates](#specifying-certificates) below for all the ways to describe certificates.

### Decryption certificates

Web APIs can request token encryption (for privacy reasons). This is even compulsory for first-party (Microsoft) web APIs that access MSA identities. The configuration property to specify the client certificates is **TokenDecryptionCertificates**. It is an array of descriptions of certificates.

#### Describing decryption certificates to use by configuration

You can express the decryption certificates in the `TokenDecryptionCertificates` property.

```Json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "msidentitysamplestesting.onmicrosoft.com",
    "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
    "ClientId": "86699d80-dd21-476a-bcd1-7c1a3d471f75",

    "TokenDecryptionCertificates": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://msidentitywebsamples.vault.azure.net",
        "KeyVaultCertificateName": "MicrosoftIdentitySamplesCert"
      }
     ]
  }
}
```

See [Specifying certificates](#specifying-certificates) below for all the ways to describe certificates.

#### Describing decryption certificates to use programmatically

You can also specify the certificate description programmatically:

```Csharp
MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
options.TokenDecryptionCertificates = new CertificateDescription[] {
 CertificateDescription.FromKeyVault("https://msidentitywebsamples.vault.azure.net",
                                     "MicrosoftIdentitySamplesCert")
};
```

See [Specifying certificates](#specifying-certificates) below for all the ways to describe certificates.

#### Helping certificate rotation by sending x5c

It's possible to specify if the x5c claim (public key of the certificate) should be sent to the STS each
time the web app or web API calls Azure AD. Sending the x5c enables application developers to achieve easy certificate
rollover in Azure AD: this method will send the public certificate to Azure AD along with the token request,
so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
This saves the application admin from the need to explicitly manage the certificate rollover
(either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni.

To specify to send the x5c claim, set the boolean `SendX5C` property of the options to true either by configuration
or programmatically.

```Json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "msidentitysamplestesting.onmicrosoft.com",
    "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
    "ClientId": "86699d80-dd21-476a-bcd1-7c1a3d471f75",

    "TokenDecryptionCertificates": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://msidentitywebsamples.vault.azure.net",
        "KeyVaultCertificateName": "MicrosoftIdentitySamplesCert"
      }
     ],
     "SendX5C": "true"
  }
}
```

### Specifying certificates

You can describe the certificates to load, either by configuration, or programmatically:

- from the certificate store (Windows) and a thumbprint ("440A5BE6C4BE2FF02A0ADBED1AAA43D6CF12E269"),
- from the certificate store (Windows) and a distinguished name ("CN=TestCert"),
- from a path on the disk and optionally a password (probably only for debugging locally),
- directly from a Base64 representation of the certificate,
- from Azure Key Vault,
- directly providing it (programmatically only).

Describing the certificate by configuration allows for just-in-time loading, rather than paying the startup cost. For instance for a web app that signs in a user, do not load the certificate until an access token is needed to call a web API.

When your certificate is in Key Vault, Microsoft.Identity.Web leverages Managed Identity, therefore enabling your application to have the same code when deployed (for instance on a VM or Azure app services), or locally on your developer box (using developer credentials).

The following sections show how to specify the client credential certificates, but the principle is the same for the decryption certificates. Just replace `ClientCertificates` with `TokenDecryptionCertificates`.

#### Getting certificates from Key Vault

##### Microsoft.Identity.Web leverages Managed Identity

To fetch certificates from KeyVault, Microsoft.Identity.Web leverages Managed Identity through the Azure SDK
[DefaultAzureCredential](https://azure.github.io/azure-sdk/posts/2020-02-25/defaultazurecredentials.html).

This works out of the box on the developer machine using the developer credentials, and also when deployed with Service fabric or App Services in Azure
provided you've been using a System-assigned Managed identity.

However:

- If you are using a User-assigned managed identity, you will need to set an environment variable AZURE_CLIENT_ID to be the
user-assigned managed identity clientID. You can do that through the Azure portal:
  1. Go to Azure App Service -> Settings | Configuration -> Application  Settings
  2. Add or update the `AZURE_CLIENT_ID` app setting to the user assigned managed identity ID.

- When used on your developer machine, you have several accounts in Visual Studio, you'll need to specify
  which account to use, by setting another environement variable `AZURE_USERNAME`

##### Specifying client certificate from Key Vault by configuration

```Json
{
    "ClientCertificates": [
      {
        "SourceType": "KeyVault",
        "KeyVaultUrl": "https://msidentitywebsamples.vault.azure.net",
        "KeyVaultCertificateName": "MicrosoftIdentitySamplesCert"
      }
     ]
  }
}
```

##### Specifying client certificate from Key Vault programmatically

```Csharp
MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
options.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromKeyVault("https://msidentitywebsamples.vault.azure.net",
                                     "MicrosoftIdentitySamplesCert")
};
```

#### Specifying certificates from a path

##### Specifying certificates from a path by configuration

```Json
{
    "ClientCertificates": [
      {
       "SourceType": "Path",
       "CertificateDiskPath": "c:\\temp\\WebAppCallingWebApiCert.pfx",
      "CertificatePassword": "password"
      }]
}
```

##### Specifying certificates from a path programmatically

```Csharp
MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
options.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromPath(@"c:\temp\WebAppCallingWebApiCert.pfx",
                                     "password")
};
```

#### Specifying certificates from a certificate store by distinguished name

##### Specifying certificates from a certificate store by distinguished name by configuration

```Json
{
    "ClientCertificates": [
      {
      "SourceType": "StoreWithDistinguishedName",
      "CertificateStorePath": "CurrentUser/My",
      "CertificateDistinguishedName": "CN=WebAppCallingWebApiCert"
      }]
}
```

##### Specifying certificates from a certificate store by distinguished name programmatically

```csharp
MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
options.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromStoreWithDistinguishedName(StoreLocation.CurrentUser,
                                     StoreName.My,
                                     "CN=WebAppCallingWebApiCert")
};
```

#### Specifying certificates from a certificate store by thumbprint

##### Specifying certificates from a certificate store by thumbprint by configuration

```Json
{
    "ClientCertificates": [
      {
       "SourceType": "StoreWithThumbprint",
       "CertificateStorePath": "CurrentUser/My",
       "CertificateThumbprint": "962D129A859174EE8B5596985BD18EFEB6961684"
      }]
}
```

##### Specifying certificates from a certificate store by thumbprint programmatically

```csharp
MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
options.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromStoreWithThumbprint(StoreLocation.CurrentUser,
                                     StoreName.My,
                                     "962D129A859174EE8B5596985BD18EFEB6961684")
};
```

#### Specifying certificates from a certificate store by Base64 encoded value

##### Specifying certificates from a certificate store by Base64 encoded value by configuration

```Json
{
    "ClientCertificates": [
      {
       "SourceType": "Base64Encoded",
       "Base64EncodedValue": "MIIDHzCCAgegA.....r1n8Czew8TPfab4OG37BuEMNmBpqoRrRgFnDzVtItOnhuFTa0="
      }]
}
```

##### Specifying certificates from a certificate store by Base64 encoded value programmatically

```csharp
MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
options.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromBase64Encoded("MIIDHzCCAgegA.....r1n8Czew8TPfab4OG37BuEMNmBpqoRrRgFnDzVtItOnhuFTa0=")
};
```

#### Specifying certificates as an X509Certificate2

You can also directly specify the certificate description as an X509Certificate2 that would you have loaded. This is only possible programmatically

```csharp
MicrosoftIdentityOptions options = new MicrosoftIdentityOptions();
options.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromCertificate(x509certificate2)
};
```

### Microsoft Identity Web classes used for certificate management

This is a class diagram showing how the classes involved in certificate management in Microsoft.Identity.Web are articulated:

  ![image](https://user-images.githubusercontent.com/13203188/84315481-06f7af00-ab6a-11ea-85fd-2aa615f79520.png)
