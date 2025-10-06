# Using certificates with Microsoft.Identity.Web

Apps leveraging MSAL or Microsoft.Identity.Web use certificates in two situations:

- In web apps, web APIs, and daemon application, to prove the identity of the application, instead of using a client secret. Other options are available as client credentials
- In web APIs, to decrypt tokens if the web API opted to get encrypted tokens.

Also certificates can be specified both by configuration, in the configuration file, or programmatically. 

This article explains both usages, and describes the certificates to use.

Table of contents:
- [Using certificates with Microsoft.Identity.Web](#using-certificates-with-microsoftidentityweb)
  - [Client certificates](#client-certificates)
    - [Describing client certificates to use by configuration](#describing-client-certificates-to-use-by-configuration)
    - [Describing client certificates to use programmatically](#describing-client-certificates-to-use-programmatically)
    - [Helping certificate rotation by sending x5c](#helping-certificate-rotation-by-sending-x5c)
  - [Decryption certificates](#decryption-certificates)
    - [Describing decryption certificates to use by configuration](#describing-decryption-certificates-to-use-by-configuration)
    - [Describing decryption certificates to use programmatically](#describing-decryption-certificates-to-use-programmatically)
  - [Ways of specifying certificates](#ways-of-specifying-certificates)
    - [Specifying certificates as an X509Certificate2](#specifying-certificates-as-an-x509certificate2)
    - [Getting certificates from Key Vault](#getting-certificates-from-key-vault)
    - [Specifying certificates](#specifying-certificates)
    - [Microsoft Identity Web classes used for certificate management](#microsoft-identity-web-classes-used-for-certificate-management)
  - [Observability of client certificates](#observing-client-certificates)

## Client certificates

Web apps and web APIs are confidential client applications.

They can prove their identity to Azure AD or Azure AD B2C by three means:

| Method              | Supported in Microsoft.Identity.Web |
| ------------------- | ----------------------------------- |
| Client secrets      | Yes, but not recommended!           |
| Client certificates | Yes                                 |
| Client assertions   | Yes                                 |

 Microsoft.Identity.Web supports specifying client certificates. The configuration property to specify the client certificates is **ClientCertificates**. It's an array of certificate descriptions. There are several ways of describing certificates. see [Specifying certificates](#specifying-certificates) below.

### Describing client certificates to use by configuration

You can express the client certificates in the **ClientCredentials** property. **ClientCredentials** can contain certificate descriptions, but also a client secret, or other forms of credentials (workload identity federation, ...).

```Json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "msidentitysamplestesting.onmicrosoft.com",
    "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
    "ClientId": "86699d80-dd21-476a-bcd1-7c1a3d471f75",
    "ClientCredentials": [
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


### Describing client certificates to use programmatically

You can also specify the certificate description programmatically. For this, you add `CertificateDescription` instances to the `ClientCertificates` property of `MicrosoftIdentityOptions`. You can then use some of the overloads of `AddMicrosoftIdentityWebApp`, using delegates to set the `MicrosoftIdentityOptions`.

For a Web app, this would look like the following:

```CSharp
using Microsoft.Identity.Web;
public class Startup
{
 // More code here
 public void ConfigureServices(IServiceCollection services)
 {
  // More code here
  services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(microsoftIdentityOptions=>
    {
      Configuration.Bind("AzureAd", microsoftIdentityOptions);
      microsoftIdentityOptions.ClientCredentials = new CredentialDescription[] {
        CertificateDescription.FromKeyVault("https://msidentitywebsamples.vault.azure.net",
                                            "MicrosoftIdentitySamplesCert")};
    })
  .EnableTokenAcquisitionToCallDownstreamApi(confidentialClientApplicationOptions=>
    {
    Configuration.Bind("AzureAd", confidentialClientApplicationOptions); 
    })
  .AddInMemoryTokenCaches();
 }
}
```

For a web API accepting encrypted tokens, the code snippet, becomes:

```CSharp
using Microsoft.Identity.Web;
public class Startup
{
 // More code here
 public void ConfigureServices(IServiceCollection services)
 {
  // More code here
  services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddMicrosoftIdentityWebApi(
     configureJwtBearerOptions =>
     {
      Configuration.Bind("AzureAd", configureJwtBearerOptions);
     }, microsoftIdentityOptions=>
     {
      Configuration.Bind("AzureAd", microsoftIdentityOptions);
      microsoftIdentityOptions.TokenDecryptionCertificates = new CertificateDescription[] {
         CertificateDescription.FromKeyVault("https://msidentitywebsamples.vault.azure.net",
                                             "MicrosoftIdentitySamplesDecryptCert")};
     })
   .EnableTokenAcquisitionToCallDownstreamApi(
     confidentialClientApplicationOptions=>
     {
      Configuration.Bind("AzureAd", confidentialClientApplicationOptions); 
     })
   .AddInMemoryTokenCaches();
 }
}
```

See [Specifying certificates](#specifying-certificates) below for all the ways to specify client  certificates.

### Helping certificate rotation by sending x5c

This is a legacy setup that is not available to 3rd parties. Please use Managed Identity or Federated Identity Credentials instead.

It's also possible to specify if the *x5c* claim (public key of the certificate) should be sent to Azure AD each
time the web app or web API calls Azure AD. Sending the x5c enables application developers to achieve easy certificate
rollover in Azure AD: this method will send the public certificate to Azure AD along with the token request,
so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
This saves the application admin from the need to explicitly manage the certificate rollover
(either via portal or PowerShell/CLI operation). For details see https://aka.ms/msal-net-sni.

Using this send x5c method does also require that the manifest for the Azure app is configured with the certificate subjectName (example below).

To specify to send the x5c claim, set the boolean `SendX5C` property of the options to true either by configuration in the appsettings file
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

To configure the Azure application to enable easy cert rotation add the trusted certificate subject names to the app manifest

```Json
  "trustedCertificateSubjects": [
        {
            "authorityId": "00000000-0000-0000-0000-000000000001",
            "subjectName": "value-in-your-CN-in-the-cert",
            "revokedCertificateIdentifiers": []
        }
    ]
```


## Decryption certificates

Web APIs can request token encryption (for privacy reasons). This is even compulsory for first-party (Microsoft) web APIs that access MSA identities. The configuration property to specify the client certificates is **TokenDecryptionCertificates**. It's an array of descriptions of certificates.

### Describing decryption certificates to use by configuration

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

### Describing decryption certificates to use programmatically

You can also specify the certificate description programmatically using the overload of `AddMicrosoftIdentityWebApi` 
that take delegate parameters, by setting the `TokenDecryptionCertificates` property
of the `MicrosoftIdentityOptions` parameter of the delegate.

```CSharp
using Microsoft.Identity.Web;
public class Startup
{
 // More code here
 public void ConfigureServices(IServiceCollection services)
 {
  // More code here
  services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddMicrosoftIdentityWebApi(
     configureJwtBearerOptions => {
      Configuration.Bind("AzureAd", configureJwtBearerOptions);
     },
     microsoftIdentityOptions=> {
      Configuration.Bind("AzureAd", microsoftIdentityOptions);
      microsoftIdentityOptions.TokenDecryptionCertificates = new CertificateDescription[] {
         CertificateDescription.FromKeyVault("https://msidentitywebsamples. vault.azure.net",
                                             "MicrosoftIdentitySamplesCert")};
     })
   .EnableTokenAcquisitionToCallDownstreamApi(
     confidentialClientApplicationOptions=> {
      Configuration.Bind("AzureAd", confidentialClientApplicationOptions); 
     })
   .AddInMemoryTokenCaches();
 }
```
The code snippets below only describe the lines used to get a certificate description to fill-in the
collection of certificate descriptions (therefore replacing the following lines from the code snippet
above:
```CSharp
        CertificateDescription.FromKeyVault("https://msidentitywebsamples. vault.azure.net",
                                            "MicrosoftIdentitySamplesCert")};
```
See [Specifying certificates](#specifying-certificates) below for all the ways to describe certificates.

### Controlling where to get the private key from.

By default, for the methods that require it, Microsoft.Identity.Web gets the private from the machine key set and doesn't write it on disk (it uses the following `X509KeyStorageFlags`: `X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet`.
From Microsoft.Identity.Web 1.7.0, it's possible to specify the `X509KeyStorageFlags` in the certificate description (both in the config file, or programmatically). if you want to use other storage flags than the default ones.

## Ways of specifying certificates
You can describe the certificates to load, either by configuration, or programmatically:
- from the certificate store (Windows) and a thumbprint ("440A5BE6C4BE2FF02A0ADBED1AAA43D6CF12E269"),
- from the certificate store (Windows) and a distinguished name ("CN=TestCert"),
- from a path on the disk and optionally a password (probably only for debugging locally),
- directly from a Base64 representation of the certificate,
- from Azure Key Vault,
- directly providing it (programmatically only).
Describing the certificate by configuration allows for just-in-time loading, rather than paying the startup cost. For instance for a web app that signs in a user, don(t load the certificate until an access token is needed to call a web API.
When your certificate is in Key Vault, Microsoft.Identity.Web uses Managed Identity, therefore enabling your application to have the same code when deployed (for instance on a VM or Azure app services), or locally on your developer box (using developer credentials).
### Specifying certificates as an X509Certificate2
You can also directly specify the certificate description as an **X509Certificate2** that would you've loaded. This is only possible programmatically, both for 
client certificates:
```csharp
microsoftIdentityOptions.ClientCertificates = new CertificateDescription[] {
 TokenDecryptionCertificates.FromCertificate(x509certificate2)
};
```
and for token decryption certificates:
```csharp
microsoftIdentityOptions.TokenDecryptionCertificates = new CertificateDescription[] {
 CertificateDescription.FromCertificate(x509certificate2)
};
```
### Getting certificates from Key Vault
To fetch certificates from KeyVault, Microsoft.Identity.Web uses Managed Identity through the Azure SDK
[DefaultAzureCredential](https://azure.github.io/azure-sdk/posts/2020-02-25/defaultazurecredentials.html).
This works seamlessly on you developer machine using your developer credentials (used in Visual Studio, Azure CLI, Azure PowerShell), and also when deployed with Service fabric or App Services in Azure
provided you've been using a System-assigned Managed identity.
However:
- If you're using a User-assigned managed identity, you'll need to set the `UserAssignedManagedIdentityClientId` configuration property or set an environment variable AZURE_CLIENT_ID to be the
user-assigned managed identity clientID. You can do that through the Azure portal:
  1. Go to Azure App Service -> Settings | Configuration -> Application  Settings
  2. Add or update the `AZURE_CLIENT_ID` app setting to the user assigned managed identity ID.
- When, on your developer machine, you have several accounts in Visual Studio, you'll need to specify
  which account to use, by setting another environment variable `AZURE_USERNAME`

### Specifying certificates
The following table shows all the ways to specify client certificates by configuration or programmatically. To
specify token decryption certificates instead of, or in addition to, client certificates, just replace `ClientCertificates` by `TokenDecryptionCertificates`.
<table>
<tr>
<td>How to get the certificate</td>
<td>By configuration</td>
<td>Programmatically</td>
</tr>
<tr>
<td>From KeyVault</td>
<td>

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
```

</td>
<td>

```CSharp
microsoftIdentityOptions.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromKeyVault("https://msidentitywebsamples.vault.azure.net",
                                     "MicrosoftIdentitySamplesCert")
};
```

</td>
</tr>
<tr>
<td>From a path</td>
<td>

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

</td>

<td>

```CSharp
microsoftIdentityOptions.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromPath(@"c:\temp\WebAppCallingWebApiCert.pfx",
                                     "password")
};
```

</td>
</tr>

<tr>
<td>By distinguished name</td>
<td>

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

</td>

<td>

```csharp
microsoftIdentityOptions.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromStoreWithDistinguishedName(StoreLocation.CurrentUser,
                                     StoreName.My,
                                     "CN=WebAppCallingWebApiCert")
};
```

</td>
</tr>

<tr>
<td>By thumbprint</td>
<td>

```Json
{
 "ClientCertificates": [
 {
  "SourceType": "StoreWithThumbprint",
  "CertificateStorePath": "CurrentUser/My",
  "CertificateThumbprint": "962D129A...D18EFEB6961684"
 }]
}
```

</td>

<td>

```csharp
microsoftIdentityOptions.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromStoreWithThumbprint(StoreLocation.CurrentUser,
                                     StoreName.My,
                                     "962D129A...D18EFEB6961684")
};
```

</td>
</tr>

<tr>
<td>By Base64 encoding</td>
<td>

```Json
{
 "ClientCertificates": [
 {
  "SourceType": "Base64Encoded",
  "Base64EncodedValue": "MIIDHzCgegA.....r1n8Ta0="
 }]
}
```

</td>

<td>

```csharp
microsoftIdentityOptions.ClientCertificates = new CertificateDescription[] {
 CertificateDescription.FromBase64Encoded("MIIDHzCgegA.....r1n8Ta0=")
};
```

</td>
</tr>
</table>

### Observing client certificates

As you probably know, if you provide a description of where Microsoft.Identity.Web can get the client certificates, instead of providing the certificate yourself, Microsoft.Identity.Web can rotate a certificate when it expires, by attempting once to get a new version from the same location.

Some of you requested a way to know:
- which client certificate is selected by Microsoft.Identity.Web token acquirer
- when a certificate is un-selected (rotated)

In Microsoft.Identity.Web 2.15.0, we introduced an experimental API (meaning to get feedback, could change in the future without taking a major version change), that provides this observability.

The way to use it is:
- Create an implementation of `ICertificatesObserver`. When Microsoft.Identity.Web selects a certificate (from the credential description collection), or un-selects one (because it was rejected by the Identity provider), the `OnClientCertificateChanged` method will be called.

  ```csharp
     void ICertificatesObserver.OnClientCertificateChanged(CertificateChangeEventArg e)
     {
       switch (e.Action)
       {
           case CerticateObserverAction.Selected:
               currentCertificate = e.Certificate;
               // Log what you want, or change the description
               break;

           case CerticateObserverAction.Deselected:
               currentCertificate = null;
                // Log what you want (from e.Certificate), or change the description (e.Description)
               break;
       }
     }
  ```

- Add it to the service collection:
  ```csharp
  tokenAcquirerFactory.Services.AddSingleton<ICertificatesObserver>(this);
  ```
 
Here are the types used for this observability.

<img width="497" alt="image" src="https://github.com/AzureAD/microsoft-identity-web/assets/13203188/93d0b1ea-1c04-4eeb-9972-27daf27fbcd6">


### Microsoft Identity Web classes used for certificate management

This is a class diagram showing how the classes involved in certificate management in Microsoft.Identity.Web are articulated:

  ![image](https://user-images.githubusercontent.com/13203188/84315481-06f7af00-ab6a-11ea-85fd-2aa615f79520.png)

## Troubleshooting 

Certificates can be expired which can result in the errors: 
  ```text
  IDW10501: Exception acquiring token for a confidential client. 
  IDW10109: All client certificates passed to the configuration have expired or can't be loaded.
  ```
You can be logged into Visual Studio, but with expired credentials. Click "account settings" in Visual Studio to view whether your credentials have expired. You may have to re-enter your credentials to resolve this issue.

![image](https://github.com/AzureAD/microsoft-identity-web/assets/69649063/4c8db38b-da3d-4a24-9a1e-b5e4b9ff4fc9)
