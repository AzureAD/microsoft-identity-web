# How to get client secrets from KeyVault

Microsoft does not encourage the usage of Client secrets. It's better to use FIC+MSI or client certificates.
Microsoft Identity Web does not support yet getting the client secrets from KeyVault.

If you really want to use client secrets and store them in KeyVault, you can use the following work around:

1. In your configuration, for the CredentialDescription, use SourceType=ClientSecret and set both KeyVaultUrl (to the URL of your KeyVault) and ClientSecret (to the name of the secret in KeyVault)

2. In your initialization code use the following:

   ```csharp
   services.Configure<MicrosoftIdentityApplicationOptions>(options =>
   {
    // Get the first credential description
    var credentials = options.ClientCredentials!.First();

    // If it's a secret, get it from KeyVault (Until IdWeb supports KeyVault secrets directly)
    if (credentials.SourceType == CredentialSource.ClientSecret && !string.IsNullOrEmpty(credentials.KeyVaultUrl))
    {
     var keyVault = new SecretClient(new Uri(keyVaultInstance), new DefaultAzureCredential());
     var secret = keyVault.GetSecret(credentials.ClientSecret).Value;
     credentials.ClientSecret = secret ;
     credentials.KeyVaultUrl = string.Empty;
    }
   });
   ```