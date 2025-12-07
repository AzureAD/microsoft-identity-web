# Logging

Microsoft Identity Web integrates with the logging available in ASP.NET Core. The MSAL .NET logs are also enabled to assist with troubleshooting and understanding any issues that may occur during token acquisition. The Microsoft.IdentityModel logs are useful to troubleshoot token validation issues.

## Subscribing to middleware events

For information about logging in middleware, see [Web API troubleshooting](https://github.com/AzureAD/microsoft-identity-web/wiki/Web-API-Troubleshooting).

## Enable logging in Microsoft.Identity.Web

To enable logging in Microsoft.Identity.Web, set a log level value for `Microsoft.Identity` in the `Logging` section of _appsettings.json_ (log levels are covered in later in this article).

For example, this enables logging in Microsoft.Identity.Web and sets the log level to informational:

`"Microsoft.Identity": "Information"`

When you configure the `Logging` section of _appsettings.json_ with a value for `Microsoft.Identity`, you enable logging in Microsoft.Identity.Web, MSAL.NET, and IdentityModel.

**Example:** `Logging` section of an _appsettings.json_ file (excerpt) that enables logging events at the informational level:

```Json
"AzureAd":
{
  ...
},
"Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Identity": "Information"
        }
...
```


## If you want to disable detailed logging.

To disable detailed logging you can set `Logging:LogLevel:Microsoft.Identity.Web` to `None` in the configuration.

```Json
"AzureAd":
{
  ...
},
"Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Identity.Web": "None"
        }
...
```


## Log levels in Microsoft.Identity.Web

MSAL.NET, and by extension Microsoft.Identity.Web, provides several log levels via the [LogLevel] enum, including:  [log level settings](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/logging#logging-in-msalnet---detailed):

- `Info` - Recommended for debugging and development, `Info` logs the key events of an authentication flow in MSAL .NET. Use with caution in production due to high volume of log events.
- `Verbose` - Recommended only for debugging and development, `Verbose` logs the most detailed messages. Use with extreme caution in production due to high volume of log events.
- `Warning` - Logs abnormal or unexpected events. Typically includes conditions that don't cause the app to fail.
- `Error` - Logs errors and exceptions.
- `None` - Logs will not be written. To avoid losing log messages from other services, set this log level using the full namespace: `"Microsoft.Identity.Web":"None"`

Log levels in MSAL.NET have equivalent log levels in ASP.NET as shown in the following table:

| MSAL.NET | ASP.NET |
|-----------------------------------------------|---------------------------------------------------|
| [Microsoft.Identity.Client.LogLevel].Info       | [Microsoft.Extensions.Logging.LogLevel].Information |
| [Microsoft.Identity.Client.LogLevel].Verbose    | [Microsoft.Extensions.Logging.LogLevel].Debug       |
| [Microsoft.Identity.Client.LogLevel].Verbose    | [Microsoft.Extensions.Logging.LogLevel].Trace       |
| [Microsoft.Identity.Client.LogLevel].Warning    | [Microsoft.Extensions.Logging.LogLevel].Warning     |
| [Microsoft.Identity.Client.LogLevel].Error      | [Microsoft.Extensions.Logging.LogLevel].Error       |
| [Microsoft.Identity.Client.LogLevel].Error      | [Microsoft.Extensions.Logging.LogLevel].Critical    |
|                                                 | [Microsoft.Extensions.Logging.LogLevel].None        |

## Logging personal identifiable information (PII)

By default, neither MSAL.NET nor Microsoft.Identity.Web log any PII or the organizational identifiable information (OII) it might contain. You must manually enable the logging of PII in these libraries.

> :warning: **WARNING: You and your application are responsible** for complying with all applicable regulatory requirements including but not limited to those set forth by the [General Data Protection Regulation (GDPR)](https://www.microsoft.com/trust-center/privacy/gdpr-overview). Before you enable logging PII, ensure you are able to safely handle this potentially highly sensitive data.

To enable logging PII in Microsoft.Identity.Web, add this line to the `AzureAd` section of _appsettings.json_:

`"EnablePiiLogging": true,`

**Example:** Excerpt of an _appsettings.json_ file that shows the `EnablePiiLogging` setting in the `AzureAd` section of the file and its default value of `false`.

```Json
"AzureAd":
{
  // WARNING: Setting this to 'true' enables logging personal identifiable information (PII) which can contain highly sensitive data.
  "EnablePiiLogging": false,
},
"Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Identity": "Information"
        }
...
```

## Logging a distributed token cache in .NET Framework or .NET Core

If you use Microsoft.Identity.Web's token cache serializers in .NET Framework or .NET Core, you can still benefit from detailed token cache logs.

To enable detailed logging for Microsoft.Identity.Web's token cache serializers in .NET Framework or .NET Core, set the [LoggerFilterOptions.MinLevel] property to [LogLevel.Debug]:

```csharp
// more code here
     app.AddDistributedTokenCache(services =>
     {
                services.AddDistributedMemoryCache();
                services.AddLogging(configure => configure.AddConsole())
                        .Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Debug);
     });
// more code here
```

To see more sample code using Microsoft Identity Web token cache serializers, see the [ConfidentialClientTokenCache code sample](https://github.com/Azure-Samples/active-directory-dotnet-v1-to-v2/blob/master/ConfidentialClientTokenCache/Program.cs) on GitHub.

## Get a correlation ID for Microsoft support

Logs can help you understand MSAL .NET's behavior on the client side, but to understand what's happening on the service side, you might need a _correlation ID_.

Correlation IDs can help Microsoft Customer Support Services (CSS) and the MSAL team troubleshoot issues by enabling them to trace authentication requests through Microsoft's back-end services.

To get a correlation ID, you can:

- Get the [AuthenticationResult.CorrelationId] property value after a successful authentication operation.
- Get the [MsalServiceException.CorrelationId] property value of an exception you've caught.
- Set your own correlation ID in Microsoft.Identity.Web's [TokenAcquisitionOptions.CorrelationId] property when you request a token.

    For example:
    
    ```csharp
    public async Task<ActionResult> Details(int id, Guid correlationId)
    {
        var value = await _downstreamWebApi.CallWebApiForUserAsync<object, Todo>(
        ServiceName,
        null,
        options =>
        {
            options.HttpMethod = HttpMethod.Get;
            options.RelativePath = $"api/todolist/{id}";
            options.TokenAcquisitionOptions.CorrelationId = correlationId;
        });
        return View(value);
    }
    ```

[AuthenticationResult.CorrelationId]: https://docs.microsoft.com/dotnet/api/microsoft.identity.client.authenticationresult.correlationid
[LoggerFilterOptions.MinLevel]:https://docs.microsoft.com/dotnet/api/microsoft.extensions.logging.loggerfilteroptions.minlevel#microsoft-extensions-logging-loggerfilteroptions-minlevel
[LogLevel.Debug]: https://docs.microsoft.com/dotnet/api/microsoft.extensions.logging.loglevel
[Microsoft.Extensions.Logging.LogLevel]: https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel
[Microsoft.Identity.Client.LogLevel]: https://docs.microsoft.com/dotnet/api/microsoft.identity.client.loglevel
[MsalServiceException.CorrelationId]: https://docs.microsoft.com/dotnet/api/microsoft.identity.client.msalserviceexception.correlationid
[TokenAcquisitionOptions.CorrelationId]: https://docs.microsoft.com/dotnet/api/microsoft.identity.web.tokenacquisitionoptions.correlationid