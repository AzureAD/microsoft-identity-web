### Getting started with Microsoft Identity Web
- [Home](https://github.com/AzureAD/microsoft-identity-web/wiki)
- [Why use Microsoft Identity Web?](Microsoft-Identity-Web-basics)
- [Web apps](web-apps)
  - [Create a new web app](new-web-app)
  - [Add Entra authentication to an existing web app](existing-web-app)
- [Web APIs](web-apis)
   - [gRPC services](grpc)
   - [Azure Functions](Azure-Functions)
   - [Get a token in an event handler](get-token-in-event-handler)
- [Minimal support for .NET FW Classic](asp-net)
   - [Package references](NuGet-package-references)
- [Logging](Logging)
- [Azure AD B2C limitations](b2c-limitations)
- [Samples](samples)

#### [Credentials](Client-Credentials)
   - [Certificates](Certificates)   
   - [Managed Identity as Federated Credential](https://github.com/AzureAD/microsoft-identity-web/wiki/Federated-Identity-Credential-(FIC)-with-a-Managed-Service-Identity-(MSI))
   - [Federated Credentials from other Identity Provider](https://github.com/AzureAD/microsoft-identity-web/wiki/Federated-Credentials-from-other-Identity-Providers)
   - **Extensibility**: [Bring your own credential](https://github.com/AzureAD/microsoft-identity-web/wiki/Custom-Signed-Assertion-Providers)
   - Get [client secrets from KeyVault](https://github.com/AzureAD/microsoft-identity-web/wiki/Client-secrets-from-KeyVault)   

#### Token cache serialization
- [Token cache serialization](token-cache-serialization)
   - [Distributed cache advanced options](L1-Cache-in-Distributed-(L2)-Token-Cache)
   - [L2 cache eviction](Handle-L2-cache-eviction)
   - [Redis with Docker](Set-up-a-Redis-cache-in-Docker)
   - [Troubleshooting](https://github.com/AzureAD/microsoft-identity-web/wiki/Token-Cache-Troubleshooting)

#### Web apps
- [Web apps](web-apps)
- [Web app samples](web-app-samples)
- [Web app template](web-app-template)
- [Call an API from a web app](adding-call-api-to-web-app)
- [Managing incremental consent and conditional access](Managing-incremental-consent-and-conditional-access)
- [Web app troubleshooting](web-app-troubleshooting)
- [Deploy to App Services Linux containers or with proxies](Deploying-Web-apps-to-App-services-as-Linux-containers)
- [SameSite cookies](SameSite-Cookies)
- [Hybrid SPA](Hybrid-SPA)

#### Web APIs 
- [Web APIs](web-apis)
- [Web API samples](web-api-samples)
- [Web API template](web-api-template)
- [Call an API from a web API](adding-call-api-to-web-app)
- [Token Decryption](Token-Decryption)
- [Web API troubleshooting](web-api-troubleshooting)
- [web API protected by ACLs instead of app roles](1.2.0#support-for-web-api-protected-by-acls-and-called-by-daemon-apps)
- [gRPC apps](grpc)
- [Azure Functions](Azure-Functions)
- [Long running processes in web APIs](get-token-in-event-handler)
- [Authorization policies](authorization-policies)
- [Generic API](generic-api)

#### Daemon scenario
- [Daemon scenarios](daemon-scenarios)
- [Worker calling APIs](worker‐app‐calling‐downstream‐apis)
- [Calling APIs with Managed Identity](calling-apis-with-managed-identity)

### Advanced topics
- [Customization](customization)
- [Logging](Logging)
- [Calling graph with specific scopes/tenant](calling-graph)
- [Multiple Authentication Schemes](multiple-authentication-schemes)
- [Utility classes](utility-classes)
- [Setting FIC+MSI](Federated-Identity-Credential-(FIC)-with-a-Managed-Service-Identity-(MSI))
- [Mixing web app and web API](Mixing-web-app-and-web-api-in-the-same-ASP.NET-core-app)
- [Deploying to Azure App Services](deploying-to-app-services)
- [Azure AD B2C issuer claim support](Azure-AD-B2C-issuer-claim-support)
- [Performance](performance)
- [specify Microsoft Graph scopes and app-permissions](1.2.0#you-can-now-specify-scopes-and-app-permissions-for-graphserviceclient)
- Integrate with [Azure App Services authentication](1.2.0#integration-with-azure-app-services-authentication-of-web-apps-running-with-microsoftidentityweb)
- [Ajax calls and incremental consent and conditional access](1.2.0#ajax-calls-can-now-participate-in-incremental-consent-and-conditional-access)
- [Back channel proxys](1.2.0#support-for-back-channel-proxys-for-the-issuer-validator-metadata)
- [Client capabilities](client-capabilities)

### Extensibility
  [Credential providers](credential-providers-extensibility)

### FAQ

### News
- [Microsoft Identity Web 2.5.0](v2.0)
- [Microsoft Identity Web 1.9.0](1.9.0)
- [Microsoft Identity Web 1.6.0](1.6.0)
- [Microsoft Identity Web 1.2.0](1.2.0)
- [Microsoft Identity Web GA (1.0.0)](1.0.0)
- [Microsoft Identity Web 0.4.0](0.4.0-preview)
- [Microsoft Identity Web 0.3.0](0.3.0-preview)
- [Microsoft Identity Web 0.1.x to 0.2.x migration](Migrating-from-0.1.x-to-0.2.x)

### Contribute
- [Overview](Contributing-Overview)
  - [Build and test](build-and-test)
- [Submit bugs and feature requests](Submit-bugs-and-feature-requests)

### Other resources
- [Reference documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.identity.web?view=azure-dotnet-preview)
- [Related MSAL.NET documentation](Related-MSAL-.NET-Documentation)