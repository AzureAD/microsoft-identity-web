# As a developer of a protected web API accepting v2 tokens, I can demand my clients to acquire Encrypted tokens to call my web API

## Why would a web API developer want their web API to receive encrypted tokens?

When a client application requests an access token on behalf of a user to call a web API, the client app developer could be tempted to crack-open the token (even if we discourage them to do so),
and therefore get access to claims about the user, which we (or the web API developer) would not want the client app to know about. This is a question of privacy, for instance, we don't want claims like the `ageGroup`, or the `puid` (MSA), or even the hardware id (`HWID`) to leak.

Therefore, web APIs can request encrypted tokens. The [Encrypted Web token standard (JWE)](https://tools.ietf.org/html/rfc7516) provides a solution to this problem.

## TokenDecryption Certificates

In the registration of the web API, you can add a decrypt certificate (sharing the public key with Azure AD), and your application has the corresponding private key. While still in the app registration, you can direct Azure AD to encrypt tokens with the decrypt certificate that you provided. 

When the client receives the access token, it will be encrypted (meaning the client cannot open it).

When the web API receives the encrypted access token from the client, it uses the decrypt certificate to decrypt the access token and validate the claims.

## How to do it

### Generate the certificate

#### Use Azure Key Vault
See the Key Vault [documentation](https://docs.microsoft.com/en-us/azure/key-vault/certificates/certificate-scenarios) on creating certificates in Key Vault.

#### Use PowerShell
Here is an example on Windows, using PowerShell to create the certificate, and the "Mange User Certificate" management console to export the public and private keys.

1. **Generate your certificate** into the certificate store

```
New-SelfSignedCertificate -Subject "CN=TestJWE" -CertStoreLocation "Cert:\CurrentUser\My" `
                          -KeyExportPolicy Exportable -KeySpec Signature
```

2. **Export the public key (to share it later with Azure AD)**. You can use the "Manage User Certificates" management console in Windows, and navigate to Personal\Certificates. Locate the "TestJWE" certificate and in All Tasks, select **Export**. 

   Select **No, do not export the private key**, keep the defaults (DER encoded binary), and provide a file name (for instance TestJWE.cer). This file will be used for the application registration. The tokens will be encrypted by Azure AD using this public key.

3. **Export the private key**. Still in the "Manage User Certificates" console select **Export** again. This time, choose **Yes, export the private key**, keep the defaults (DER encoded binary), provide a password and provide a file name (for instance TestJWE.pfx). This file will be used to deploy the private key to the web API. Only your web API (which is a confidential client app) will know the private key, and therefore only your web API will be able to decode the encoded JWE token.

### In the Azure AD Portal

In the registration for the web API, under **Certificates & Secrets**, click on **Upload certificate** and upload the certificate that was created in step 1 above (for instance, TestJWE.cer). 

This certificate has an ID associated with it, 

Now, in the navigation sidebar, go to the web API **manifest**. In the **keyCredentials** section, you will need to do the following:
1. Create a new **keyId**, you can use Visual Studio -> tools -> create new GUID for this. Copy the value and save it in Notepad.
2. Replace the **keyId** value with the new GUID you just generated
3. Replace the `"usage": "Verify"` with `"usage": "Encrypt"`. See [Usages of the certificate](#usages-of-the-certificate) for more information.
4. Save the manifest

For example:
```Json
"keyCredentials": [
	{
	"customKeyIdentifier": "0C8862BF6F....32A8B3BFB",
		"endDate": "2021-12-14T03:56:09Z",
		"keyId": "GUID",
		"startDate": "2020-12-14T03:36:09Z",
		"type": "AsymmetricX509Cert",
		"usage": "Encrypt",
		"value": "MIIC/jCCAeagAwI....",
		"displayName": "CN=TestJWE"
	}
],
```

### Back in your web API
Follow the guidance [here](https://github.com/AzureAD/microsoft-identity-web/wiki/Certificates#decryption-certificates) to connect a certificate for token decryption to your web API. Here we suppose you have moreover uploaded your decrypt certificate to KeyVault.

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

Now when you call the web API, the access token will be encrypted.

#### Helping with certificate rotation

It's possible to specify if the x5c claim (public key of the certificate) should be sent to Azure AD each time the web app or web API calls Azure AD. Sending the x5c enables application developers to achieve easy certificate rollover in Azure AD. For details see [Helping certificate rotation by sending x5c](Certificates#helping-certificate-rotation-by-sending-x5c)

### More information on [Token Decryption Certificates](https://github.com/AzureAD/microsoft-identity-web/wiki/Certificates#decryption-certificates) with Microsoft Identity Web.

### Usages of the certificate
Usages are:

* **verify** : the private key is held by confidential clients applications to prove the application identity. Azure AD verifies this identity using the public key. verify is the default today when one uploads a certificate in the Secrets & certificates page for an app.
* **encrypt**: the public key is used by Azure AD to encrypt a token (this is the scenario explained in this document), and the private key is used by the application which is the audience of this token to decrypt the token
* **sign**: this requires the private key to be shared with Azure AD. It's used for custom token signing keys. The two main usages are gallery apps and custom claim mapping policies. An app who has configured a custom token signing key will actually receive tokens signed with that key rather than the global ESTS token signing key. Gallery apps do this because updating the key every 6 weeks is a pain, while custom claims mapping policies does this to draw a security boundary around the tenant+app pair.
* **decrypt**: also requires the private key to be shared with Azure AD.

### Additional information
See [blog post](https://damienbod.com/2020/10/22/using-encrypted-access-tokens-in-azure-with-microsoft-identity-web-and-azure-app-registrations/) by @damienbod for additional information.