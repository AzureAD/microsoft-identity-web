# Client capabilities

MSAL.NET has a way of expressing client capabilities. This is needed for scenarios such as [Conditional access evaluation](https://docs.microsoft.com/azure/active-directory/develop/app-resilience-continuous-access-evaluation)

## How to express client capabilities

The  `ConfidentialClientApplicationOptions` expose the [`ClientCapabilities`](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/7f9974cc0525d308e091dbf380ef6d8c75aad8d9/src/client/Microsoft.Identity.Client/AppConfig/ApplicationOptions.cs#L123) property

Therefore you can express them in the appsettings.json:

```Json
"AzureAD" : 
{
 // usual members
 "ClientCapabilities" : [  "cp1" ]
}
```

or, programmatically, through the options you set in `.EnableTokenAcquisitionToCallDownstreamApis`