Microsoft Identity Web allows the use of custom credential sources providers by enabling extensions of the built-in credential loaders with your own custom SDK implementation. You bring the signed credential; we do the rest.

A working sample can be found [here](https://github.com/AzureAD/microsoft-identity-web/tree/9bd521186bf9b00a2af4fc920be8c7f87683a012/tests/E2E%20Tests/CustomSignedAssertionProviderTests) in the Microsoft Identity Web repo which doubles as an integration test for this feature (The test file would not be part of the extension, but shows the code that developers using your extension would write to use it)

## Creating Your Custom Extension
Building an extension to go on top of Microsoft Identity Web can be done with just three steps (3 files likely to be added to a new project: your extension project):

### Step 1: Create an assertion provider class deriving from [ClientAssertionProviderBase](https://github.com/AzureAD/microsoft-identity-web/blob/9bd521186bf9b00a2af4fc920be8c7f87683a012/src/Microsoft.Identity.Web.Certificateless/ClientAssertionProviderBase.cs).
  - Your assertion provider takes in the properties and options needed to request a signed assertion from your custom source.
  - Your assertion provider then receives the signed assertion from your custom source and returns it. Microsoft.Identity.Web takes care of caching.
  - The sample file can be found [here](https://github.com/AzureAD/microsoft-identity-web/blob/9bd521186bf9b00a2af4fc920be8c7f87683a012/tests/E2E%20Tests/CustomSignedAssertionProviderTests/MyCustomSignedAssertionProvider.cs).


   **Example:**

   ```csharp
    namespace MyCustomExtension
    {
        internal class MyCustomSignedAssertionProvider : ClientAssertionProviderBase
        {
            public MyCustomSignedAssertionProvider(Dictionary<string, object>? properties)
            {
                // Here you would implement the logic to extract what you need from the properties passed in
                // the configuration. You could have other parameters to your constructor too (see below)
            }
            protected override Task<ClientAssertion> GetClientAssertionAsync(AssertionRequestOptions? assertionRequestOptions)
            {
                // Here you would implement the logic to get the signed assertion, which is probably going
                // to be a call to a service. This call can be parameterized by the parameters in the properties
                // of the constructor.
  
                // In this sample code we just create a fake signed assertion and return it, with its expiry
                var clientAssertion = new ClientAssertion("FakeAssertion", DateTimeOffset.Now);
                return Task.FromResult(clientAssertion);
            }
        }
    }
   ```

### Step 2: Provide a signed assertion loader class ironically implementing [ICustomSignedAssertionProvider](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/blob/6b0ae69794ff8893b136e57ace83524705becd4b/src/Microsoft.Identity.Abstractions/ApplicationOptions/ICustomSignedAssertionProvider.cs#L13).

  - Your custom assertion loader will use instances of your custom assertion provider to get signed assertions from your custom source.
  - Your loader will be called from the Microsoft Identity Web library through the LoadIfNeededAsync method for relevant credential descriptions.
  - Your loader will need to call GetSignedAssertionAsync from your provider in order to get the signed assertion.
  - If you successfully obtain a signed assertion, set the CredentialDescription.CachedValue to the new MyCustomSignedAssertionProvider.
  - If there is an exception, set the skip value in the credential description to true to avoid trying to get the same credential again.
  - It is ok to not get a credential, not all credentials are available in all contexts a given program runs in (for instance Managed Identity
    is only available on Azure machines)
  - *Important*: only throw the exception if you want the Microsoft Identity Web library to also throw the exception, likely crashing the program.
  - The sample file can be found [here](https://github.com/AzureAD/microsoft-identity-web/blob/9bd521186bf9b00a2af4fc920be8c7f87683a012/tests/E2E%20Tests/CustomSignedAssertionProviderTests/MyCustomSignedAssertionLoader.cs).


   **Example:**

   ```csharp
    namespace MyCustomExtension
    {
        internal class MyCustomSignedAssertionLoader : ICustomSignedAssertionProvider
        {
            public MyCustomSignedAssertionLoader(ILogger<DefaultCredentialsLoader> logger)
            {
                _logger = logger;
            }
            public CredentialSource CredentialSource => CredentialSource.CustomSignedAssertion;
    
            public string Name => "MyCustomExtension";
    
            private ILogger<DefaultCredentialsLoader> _logger;
    
            public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters = null)
            {
                MyCustomSignedAssertionProvider? signedAssertion = credentialDescription.CachedValue as MyCustomSignedAssertionProvider;
                if (credentialDescription.CachedValue == null)
                {
                    signedAssertion = new MyCustomSignedAssertionProvider(credentialDescription.CustomSignedAssertionProviderData);
                }
                try
                {
                    // Given that not all credentials are available in all contexts (like managed identities not being available on local machines),
                    // we need to attempt to get a signed assertion, but if it fails, keep trying to find a working credential.
                    _ = await signedAssertion!.GetSignedAssertionAsync(null);

                    // Be sure to set the CachedValue in the CredentialDescription object to your signed assertion so you don't reevaluate
                    // the same credential more than is necessary.
                    credentialDescription.CachedValue = signedAssertion;
                }
                catch (Exception)
                {
                    // Setting the skip to true will tell the program to no longer try loading credentials 
                    // from this specific CredentialDescription object instance.
                    credentialDescription.Skip = true;

                    // Only throw the Exception if you want it to also be thrown by the Microsoft Identity Web library.
                    // Use the logger if you only want to log it.
                    throw;
                }
            }
        }
    }
   ```

### Step 3: Write an IServiceCollection extension method to register the custom loader from step 2.
  - This is what enables Microsoft Identity Web to find your custom loader
  - This is the only thing a developer using your extension will need to call in order to use it
  - The sample file can be found [here](https://github.com/AzureAD/microsoft-identity-web/blob/9bd521186bf9b00a2af4fc920be8c7f87683a012/tests/E2E%20Tests/CustomSignedAssertionProviderTests/CustomSignedAssertionProviderExtensions.cs).


   **Example:**

   ```csharp
    namespace MyCustomExtension
    {
        public static class CustomSignedAssertionProviderExtensions
        {
            public static IServiceCollection AddCustomSignedAssertionProvider(
               this IServiceCollection services)
           {
               services.AddSingleton<ICustomSignedAssertionProvider, MyCustomSignedAssertionLoader>();
               return services;
           }
        }
    }
   ```

## Using Your Custom Extension
For a user to use your extension two things need to happen.

### First: the correct configuration details need to be in place.
  - In the example below this is done using the call to tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>
  - This can instead be done in the appsettings.json which you can find an example of in the sample [here](https://github.com/AzureAD/microsoft-identity-web/blob/9bd521186bf9b00a2af4fc920be8c7f87683a012/tests/E2E%20Tests/CustomSignedAssertionProviderTests/appsettings.json)

### Second: call your custom IServiceCollection extension (in the service initialization on ASP.NET Core or from the TokenAcquirerFactory.Services elsewhere)
  - If you are not on ASP.NET Core, be sure to call it before building the TokenAcquirerFactory.
  - Never use the TokenAcquirerFactory on ASP.NET Core: you already have a service collection.
  - In the sample this is done as part of an end to end test [here](https://github.com/AzureAD/microsoft-identity-web/blob/9bd521186bf9b00a2af4fc920be8c7f87683a012/tests/E2E%20Tests/CustomSignedAssertionProviderTests/CustomSignedAssertionProviderExtensibilityTests.cs)


   ```csharp
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "msidlab4.onmicrosoft.com";
                options.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                options.ClientCredentials = [ 
                 new CredentialDescription() 
                 { 
                    SourceType = CredentialSource.CustomSignedAssertion,
                    CustomSignedAssertionProviderName = "MyCustomExtension",
                    CustomSignedAssertionProviderData = { {"key", "value"} }
                 }
                 ];
            });
            tokenAcquirerFactory.Services.AddCustomSignedAssertionProvider();
            var serviceProvider = tokenAcquirerFactory.Build();
   ```

