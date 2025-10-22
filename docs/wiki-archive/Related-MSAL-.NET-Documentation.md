# Learn more about the library

You can learn more about the tokens by looking at the following articles in MSAL.NET's conceptual documentation:

- The [Authorization code flow](https://aka.ms/msal-net-authorization-code), is invoked when the user signs in with Open ID Connect through ASP .NET Core. Then MSAL will redeem the code to get a token, which will be cached for later use.
- [AcquireTokenSilent](https://aka.ms/msal-net-acquiretokensilent), used by the controller to get an access token for the downstream API.
- [Token cache serialization](msal-net-token-cache-serialization)

Token validation is performed by the classes in the [Identity Model Extensions for .NET](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet) library. Learn about customizing
token validation by reading:

- [Validating Tokens](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/wiki/ValidatingTokens) in that library's conceptual documentation.
- [TokenValidationParameters](https://docs.microsoft.com/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters?view=azure-dotnet)'s reference documentation.
