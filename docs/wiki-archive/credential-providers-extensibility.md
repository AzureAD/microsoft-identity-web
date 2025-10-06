# How to extend Microsoft.Identity.Web credential providers

## Credential providers

Credential providers are used to take information from [CredentialDescription]() and get a credential to be used as a client or decrypt credential.

## How to extend:

```diff
    ServiceCollection services = new ServiceCollection();
            services.AddTokenAcquisition();
            services.AddHttpClient();
            services.AddInMemoryTokenCaches()
+            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICredentialSourceLoader, MyCredentialSourceLoader>());

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            var credentialLoader = serviceProvider.GetRequiredService<ICredentialsLoader>();

            CredentialDescription? cd = await credentialLoader.LoadFirstValidCredentialsAsync(new List<CredentialDescription>
            {
+               new CredentialDescription
+               {
+                   SourceType = CredentialSource.ManagedCertificate
+               }
+           }
              );
```

See also [Custom-Signed-Assertion-Providers](./Custom-Signed-Assertion-Providers)