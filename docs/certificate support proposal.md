# Why? 

## Client Certificates 
Web apps and Web APIs are confidential client applications.

They can prove their identity to Azure AD by 3 means:
- client secrets
- client certificates
- client assertions

Today, Microsoft.Identity.Web enables developers to provide client secrets.
In addition to Client secret, we'd want Microsoft.Identity.Web to support client certificates. The constraints are the following:
- enable several ways of getting the certificate. You'd provide a description on how to get the certificate.
  - from the certificate store (Windows) and a thumbprint ("440A5BE6C4BE2FF02A0ADBED1AAA43D6CF12E269")
  - from the certificate store (Windows) and a distinguished name ("CN=TestCert")
  - from a path on the disk (probably only for debugging locally)
  - directly from a base64 representation of the certificate
  - from a KeyVault address.
- getting the certificate just in time, rather than paying the startup cost. For instance for a web app that signs in a user, do not load the certificate until an access token is needed to call a Web API.
- when the certificate is stored in KeyVault, leverage Managed identity (probably though the Azure SDK for .NET)
- help you rotating your certificates but letting you provide several (2) certificates

## Decrypt certificates

It's a different topic, but still touching on certificates: Web APIs can request token encryption (for privacy reasons). This is even compulsory for 1P Web APIs that access MSA identities. We also want to let you pass decryption certificates with the same flexibility as for client certificates.

## Proposal 

The current proposal is to have the following options to specify both client certificates and decrypt certificates from the configuration file.

```Json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "msidentitysamplestesting.onmicrosoft.com",
    "TenantId": "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab",
    "ClientId": "86699d80-dd21-476a-bcd1-7c1a3d471f75",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath ": "/signout-callback-oidc",

    // To call an API
    "ClientSecret": "[Copy the client secret added to the app from the Azure portal]",

    // Exclusive with "ClientSecret"
    "ClientCertificates": [
      {
        "CertificateFromThumbprint": {
          "StoreLocation": "CurrentUser", // Optional, default = CurrentUser
          "StoreName": "My",              // Optional, default = My
          "certificateThumbprint": "440A5BE6C4BE2FF02A0ADBED1AAA43D6CF12E269"
        }
      },
      {
        "CertificateFromSubjectDistinguishedName": {
          "StoreLocation": "CurrentUser", // Optional, default = CurrentUser
          "StoreName": "My",              // Optional, default = My
          "certificateSubjectDistinguishedName": "CN=TestCert"
        }
      },
      {
        "CertificateFromPath": "C:\Users\myname\Documents\TestJWE.pfx",
        "CertificatePassword": "TestJWE"
      },
      {
        "CertificateFromBase64Encoded": "Base64Encoded"
      },
      {
        "CertificateFromKeyVault": "https://vaultname.vault.azure.net/certificates/certificateid/3fb1c62f74b844b0a2d9f1a3d289648d"
      }
    ],
    "TokenDecryptionCertificates": [
      {
        // Same possibilities as for the client certificates
      }
    ],

    "SingletonTokenAcquisition" :  true

  },
  "TodoList": {
    /*
      TodoListScope is the scope of the Web API you want to call. This can be: "api://a4c2469b-cf84-4145-8f5f-cb7bacf814bc/access_as_user",
      - a scope for a V2 application (for instance api://b3682cc7-8b30-4bd2-aaba-080c6bf0fd31/access_as_user)
      - a scope corresponding to a V1 application (for instance <GUID>/user_impersonation, where  <GUID> is the
        clientId of a V1 application, created in the https://portal.azure.com portal.
    */
    "TodoListScope": "api://a4c2469b-cf84-4145-8f5f-cb7bacf814bc/access_as_user",
    "TodoListBaseAddress": "https://localhost:44351"

  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}

```

## Todo:
- provide feedback on this proposal. Would we rather want to have a service to get certificates?
- Come-up with MicrosoftIdentityOptions to read this
- Read the certificate from its description.
- Apply the certificate to ConfidentialClientApplication.

See also [Sample code to load certiticates](https://github.com/AzureAD/microsoft-identity-web/wiki/Spec-certificates#sample-code-to-load-certificates) - which is not necessarily what we want to do, but gives an idea on manipulate the certs?

See also 
- https://github.com/AzureAD/microsoft-identity-web/issues/72
- https://github.com/AzureAD/microsoft-identity-web/issues/12


## Proposal/Principles
Initially let's reduce the problem by saying that:
- we want to support both ClientCertificates and EncryptionCertificate collections in the configuration (in MicrosoftIdentityOptions)
- however initially we'll only consider the **first** certificate of each collection. We would not handle the rotation of certificate yet, as this would need to be pushed to MSAL.NET (for the ClientCertificate), and to Wilson (for the EncyrptionCertificate). We just add the collection so that we don't break the public API later.
- we would start by only supporting certificates obtained from KeyVault, as this is the use case that most customers are after. But we'd propose a design that would work with other source of certificates (local file, base64 encoded are the ones I've seen the most)

Proposed sequence:
1. Experiment with the KeyVault SDK part of the Azure SDK. This has the advantage of getting the secrets from KeyVault both locally, and when the Web app or Web API is deployed, as it leverages Managed identity. See https://docs.microsoft.com/en-us/azure/key-vault/general/tutorial-net-create-vault-azure-web-app#update-the-code (this one is for client secrets, we need to find the equivalent with certificates, for instance by looking at https://docs.microsoft.com/en-us/dotnet/api/overview/azure/security.keyvault.certificates-readme?view=azure-dotnet#retrieve-a-certificate). We could use, for the client the DefaultAzureCredentials (which also work well in the dev environment)

   This means we would now leverage the following NuGet packages: **Azure.Identity** and **Azure.Security.KeyVault.Certificates**. This seems fine but we need to check if there is a negative impact.

1. Create a new class  `CertificateDescription` which would direct Microsoft.Identity.Web on how to get the certificate. It could be:
   - SourceType: an enumeration (KeyVault, Base64Encoded, FromPath, FromStoreWithThumbprint, FromStoreWithDistinguishedName,)
   - Container a string which would contain the container (for instance the keyVaultUrl in the case of the enum being KeyVault, or the Store Key in the case of the Windows cert Store [CurrentUser/My], the path in the case of a certificate from its path)
  - ReferenceOrValue: a string which would contain the secret identifier or value. This would be the name of the certificate in the case of keyvault, the password in the case of a path on disk, the base64 encoded value in the case of Base64 encoded.
1. Modify the `MicrosoftIdentityOptions` to add the 2 collections of `CertificateDescription` `ClientCertificates` and `EncryptionCertificate` 
2. Add a new class to load the certificates as a function of their type. We probably want to have a kind of base class (CertificateLoader), and derived classes for each SourceType. We'd start only by the KeyVault one.

Let's start simple, increment based on feedback

Possible design to discuss:

```CSharp
   /// <summary>
    /// Source for a certificate.
    /// </summary>
    public enum CertificateSource
    {
        /// <summary>
        /// KeyVault
        /// </summary>
        KeyVault,

        /// <summary>
        /// Base 64 encoded directly in the configuration.
        /// </summary>
        Base64Encoded,

        /// <summary>
        /// Local path on disk
        /// </summary>
        Path,

        /// <summary>
        /// From the certificate store, described by its thumprint.
        /// </summary>
        StoreWithThumbprint,

        /// <summary>
        /// From the certificate store, described by its Distinguished name.
        /// </summary>
        StoreWithDistinguihedName,
    }

    /// <summary>
    /// Description of a certificate.
    /// </summary>
    public class CertificateDescription
    {
        /// <summary>
        /// Type of the source of the certificate.
        /// </summary>
        public CertificateSource SourceType { get; set; }

        /// <summary>
        /// Container in which to find the certificate.
        /// <list type="bullet">
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.KeyVault"/>, then
        /// the container is the KeyVault base URL</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Base64Encoded"/>, then
        /// this value is not used</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Path"/>, then
        /// this value is the path on disk where to find the certificate</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithDistinguihedName"/>,
        /// or <see cref="CertificateSource.StoreWithThumbprint"/>, then
        /// this value is the path to the certificate in the cert store, for instance <c>CurrentUser/My</c></item>
        /// </list>
        /// </summary>
        public string Container { get; set; }

        /// <summary>
        /// Reference to the certificate or value.
        /// </summary>
        /// <list type="bullet">
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.KeyVault"/>, then
        /// the reference is the name of the certificate in KeyVault (maybe the version?)</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Base64Encoded"/>, then
        /// this value is the base 64 encoded certificate itself</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.Path"/>, then
        /// this value is the password to access the certificate (if needed)</item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithDistinguihedName"/>,
        /// this value is the path to the certificate in the cert store, for instance <c>CurrentUser/My</c></item>
        /// <item>If <see cref="SourceType"/> equals <see cref="CertificateSource.StoreWithThumbprint"/>,
        /// this value is the path to the certificate in the cert store, for instance <c>CurrentUser/My</c></item>
        /// </list>
        public string ReferenceOrValue { get; set; }
    }

   public class MicrosoftIdentityOptions : OpenIdConnectOptions
    {
     // Other properties here

        /// <summary>
        /// Description of the certificates used to prove the identity of the Web app or Web API.
        /// </summary>
        public CertificateDescription[] ClientCertificates { get; set; }

        /// <summary>
        /// Description of the certificates used to decrypt an encrypted token in a Web API.
        /// </summary>
        public CertificateDescription[] DecryptCertificates { get; set; }
    }

```
