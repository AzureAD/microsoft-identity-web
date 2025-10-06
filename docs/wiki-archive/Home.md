[Microsoft Identity Web](https://www.nuget.org/packages/Microsoft.Identity.Web) is a library which contains a set of reusable classes used in conjunction with ASP.NET Core, or OWIN for integrating with the [Microsoft identity platform](https://learn.microsoft.com/azure/active-directory/develop/) (formerly *Azure AD v2.0 endpoint*), [AAD B2C](https://learn.microsoft.com/azure/active-directory-b2c/), and [Microsoft Entra External ID](https://www.microsoft.com/security/business/identity-access/microsoft-entra-external-id)

This library is for specific usage with:

- [Web applications](web-apps), which sign in users and, optionally, call web APIs
- [Protected web APIs](web-apis), which optionally call protected downstream web APIs
- [Daemon scenarios](daemon-scenarios), where an apps calls a protected API on behalf of itself (not a user)


 [Conceptual documentation](#conceptual-documentation)
  - [Getting started with Microsoft Identity Web](#getting-started-with-microsoft-identity-web)
    - [Details on signing-in users with a web app](web-apps)
    - [Details on calling protected web APIs](web-apis)
    - [Details on token cache serialization](token-cache-serialization)
    - [Create a ASP .NET Core web API template with Microsoft Identity Web](#asp-net-core-web-api-template)
    - [Use JSON schema to make configuration easier](#use-json-schema-to-make-configuration-easier)
- [Roadmap](#roadmap)
- [Samples](#samples)

## Conceptual documentation

### Getting started with Microsoft Identity Web

See [Why use Microsoft.Identity.Web?](Microsoft-Identity-Web-basics)

#### Microsoft.Identity.Web NuGet package

Microsoft.Identity.Web is available as a set of NuGet packages ([Microsoft.Identity.Web](https://www.nuget.org/packages?q=microsoft.identity.web)) for .NET 6+ and OWIN. Web apps can also use the ([Microsoft.Identity.Web.UI](https://www.nuget.org/packages?q=microsoft.identity.web.ui)) NuGet package, and there are packages to help you call Microsoft Graph ([Microsoft.Identity.Web.GraphServiceClient](https://www.nuget.org/packages?q=microsoft.identity.web.graphserviceclient) or a downstream API ([Microsoft.Identity.Web.DownstreamApi](https://www.nuget.org/packages?q=microsoft.identity.web.downstreamapi). Microsoft.Identity.Web also brings solutions for client credentials (certificates, and federation identity credentials), and decrypt credentials.

#### Use JSON schema to make configuration easier

Using an appsettings.json file is the easiest way to configure your authentication. [Adding the Microsoft.Identity.Web JSON schema](https://github.com/AzureAD/microsoft-identity-web/wiki/Using-Id-Web's-JSON-Schema) to your appsettings.json file further improves ease of configuration.

#### ASP .NET Core web app and web API project templates

You can create new web apps and web APIs using the Microsoft identity platform (formerly Azure AD v2.0) or Azure AD B2C, and leveraging Microsoft.Identity.Web. For this:
- use the following `dotnet new` commands.

Audience: users to sign-in:

- AAD = Work or School accounts
- MSA = Personal Microsoft accounts
- B2C = Social accounts or local accounts (Azure AD B2C)

| Application   | Audience                                     | Dotnet new command                     |
|---------------|----------------------------------------------|----------------------------------------|
| Web API       | AAD - single tenant      | `dotnet new webapi --auth SingleOrg`    |
| Web API       | B2C             | `dotnet new webapi --auth IndividualB2C` |
| Razor Web app | AAD - single tenant      | `dotnet new webapp --auth SingleOrg`     |
| Razor Web app | AAD + MSA | `dotnet new webapp --auth MultiOrg`      |
| Razor Web app | B2C              | `dotnet new webapp --auth IndividualB2C` |
| MVC Web app | AAD - single tenant      | `dotnet new mvc --auth SingleOrg`     |
| MVC Web app | AAD + MSA | `dotnet new mvc --auth MultiOrg`      |
| MVC Web app | B2C             | `dotnet new mvc --auth IndividualB2C` |

## Roadmap

Date | Release | Blog post| Main features
------| ------- | ---------| ---------
*(Not Started)* | *Microsoft Identity Web vFuture* | |
*(Next/In progress)* | [See milestones](https://github.com/AzureAD/microsoft-identity-web/milestones) | | 
*Releases* | [All releases](https://github.com/AzureAD/microsoft-identity-web/releases/tag) |   | 
July 23, 2024 | [3.0.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/3.0.1) |  | Updated Microsoft.IdentityModel.* packages to  8.0.1.
July 18, 2024 | [3.0.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/3.0.0) |  | Updates to address [CVE-2024-30105](https://github.com/advisories/GHSA-hh2w-p6rv-4g7w). Updated Microsoft.IdentityModel.* packages to 8.0.0, Microsoft.Identity.Lab API  to 1.0.2, and Microsoft.Identity.Abstractions to 6.0.0.
June 19, 2024 | [3.0.0-preview3](https://github.com/AzureAD/microsoft-identity-web/releases/tag/3.0.0-preview3) |  | Updated Microsoft.IdentityModel.* to 8.0.0-preview3.
June 11, 2024 | [3.0.0-preview2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/3.0.0-preview2) |  | Changed `GetSignedAssertion` API, updated to `.NET 9 Preview 4`,  updated MSAL .Net to 4.61.3, and updated Azure.Identity to 1.11.4.
April 29, 2024 | [3.0.0-preview1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/3.0.0-preview1) |  | Remove support for `netcoreapp3.1`, support for `net 5.0` in the Microsoft.Identity.Web.UI package, added support for `.net9.0-preview`, added processing for `AcceptHeader` and `ContentType`, and added target Microsoft.IdentityModel 7x in OWIN targets.
July 20, 2024 | [2.21.1](https://github.com/AzureAD/microsoft-identity-web/blob/rel/v2/changelog.md#2210) |  | Updated to Microsoft.IdentityModel 7.7.1
July 19, 2024 | [2.21.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.21.0) |  | Updated to Microsoft.IdentityModel 7.7.0 and package updates to address [CVE-2024-30105](https://github.com/advisories/GHSA-hh2w-p6rv-4g7w)
June 28, 2024 | [2.20.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.20.0) |  | Updated to Microsoft.Identity.Abstractions 6.0.0
June 10, 2024 | [2.19.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.19%2C1) |  | Updated MSAL.Net to 4.61.3 and Azure.Identity to 1.11.4
May 29, 2024 | [2.19.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.19.0) |  | Added `.withUser` modifier to Microsoft Graph queries, enabled implementation of custom `IAuthorizationHeaderProvider`, added extra query parameter processing, stopped logger from initializing when log level is set to `None`, improved `GraphAuthenticationProvider` URI validation, and added processing for error code AADSTS1000502 to `TokenAcquisition`.
May 21, 2024 | [2.18.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.18.2) |  | Added ability to disable `KeyVaultCertificateLoader` with an environment variable, token acquisition in `ASP.NET Core 2.x` on `net472` & `net48`, implements `ITokenAquirerFactory`, and added target Microsoft.IdentityModel 7x in OWIN targets.
April 25, 2024 | [2.18.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.18.1) |  | Fix for FIC and updated to Microsoft.IdentityModel.* 7.5.1.
April 22, 2024 | [2.18.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.18.0) |  | Support for managed identity with federated identity credential, improved support for registering multiple downstream APIs, made `TokenAcquirerFactory` thread-safe improving support for multi-region Azure usage, and updated Microsoft.Identity.Abstractions and Azure.Security dependencies.
April 16, 2024 | [2.17.5](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.17.5) |  | Updated to MSAL 4.59.1.
March 29, 2024 | [2.17.4](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.17.4) |  | Bugfix for assertions in `TokenAcquisition`.
March 27, 2024 | [2.17.3](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.17.3) |  | Updated to Microsoft.IdentityModel.* 7.5.0.
March 14th, 2024 | [2.17.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.17.2) | | Added support for CIAM custom user domains.
January 9th, 2024 | [2.16.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.16.1) | | Update IdentityAbstractions and IdentityModel dependencies, bug fixes.
October 18th | [2.15.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.15.2) | | Updates IdentityModel dependencies for net8.0 rc2 target framework and bug fixes.
October 5th | [2.15.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.15.1) | | Update IdentityModel dependencies, bug fixes, new TokenAquirerFactory feature, and added an experimental API.
September 21st | [2.14.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.140) | | Update to Abstractions 5.0.0 and Bug fixes
September 7th | [2.13.4](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.13.4) | | Bug fixes and package updates
August 16th | [2.13.3](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.13.3) | | Addressed issues [#2351](https://github.com/AzureAD/microsoft-identity-web/issues/2351) and [#2371](https://github.com/AzureAD/microsoft-identity-web/issues/2371), updated Wilson to 7.0.0-preview2, and ASP.NET Core 3.1 as well as Net 5+ now use the DefaultTokenAcquisitionHost instead of the Asp.NET specific corollary.
June 15th | [2.12.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.12.2) | | Support for MS Graph SDK v5 via Microsoft.Identity.Web.GraphServiceClient and Microsoft.Identity.Web.GraphServiceClientBeta. See [Readme.md](https://github.com/AzureAD/microsoft-identity-web/blob/master/src/Microsoft.Identity.Web.GraphServiceClient/Readme.md) for details.
May 15th | [2.11.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.11.0) | | Support for [trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained), Update to MSAL.NET 4.54.0
May 5th | [2.10.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.10.0) | | Improved logging in downstream API, CIAM updates, and OBO support for composite tokens, plus update to Wilson 6.30.
April 14th | [2.9.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.9.0) | | A workaround to address a [breaking change in ASP.NET Core 6](https://github.com/dotnet/razor/issues/7577) with the Razor pages & update to MSAL.NET 4.53 and Wilson 6.29.
April 14th | [2.8.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.8.0) | | ID Web works with Authority in place of Tenant ID and Domain, ID Web now supports CIAM authorities.
March 30th | [2.7.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.7.0) | | `MicrosoftIdentityAppCallsWebApiAuthenticationBuilder` is now available on netstandard2.0, Id Web now supports expressing the cache key used for serializing/deserializing, bug fixes.
March 23rd | [2.6.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.6.1) | | `GetClientAssertion` is now public, Id Web now uses `TryAdd` instead of `Add` in the InMemory and Distributed caches, Id Web now supports MsAuth10ATPop, bug fixes.
February 27th|[2.5.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/2.5.0)||[Find more details here](https://github.com/AzureAD/microsoft-identity-web/wiki/v2.0), v2 brings a variety of new higher-level APIs, including support for .NET Framework (Owin), Daemon scenarios, and the new DownstreamApi.
September 19th | [2.0.0-preview](https://github.com/AzureAD/microsoft-identity-web/releases/tag/v2.0.0-preview) | | [Details here](https://github.com/AzureAD/microsoft-identity-web/wiki/v2.0).
August | [1.25.3](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.25.3) | | package updates.
July | [1.25.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.25.2) | | package updates.
June 22nd | [1.25.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.25.1) | | IIdentityLogger support, bug fixes.
June 3rd | [1.25.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.25.0) | | RequiredScopeOrAppPermissionAttribute support, bug fixes.
April 26th | [1.24.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.24.1) | | Bug fixes.
April 23rd| [1.24.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.24.0) | | Certless auth support.
March 23rd| [1.23.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.23.1) | | Bug fixes.
Feb 14th | [1.23.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.23.0) | | Hybrid spa support and update to MSAL.NET 4.41.
Jan 30th | [1.22.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.22.2) | | Bug fixes.
Jan 7th | [1.22.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.22.1) | | Update to MSAL.NET 4.40.
Jan 7th | [1.22.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.22.0) | | Ability to set request headers in IDownstreamWebApi, proof of concept for MSI, cache improvements.
Dec 3rd | [1.21.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.21.1) | | Dependent packages updates. 
Nov 19th | [1.21.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.21.0) | | Bug fixes and support long running process for OBO. 
Nov 4th | [1.20.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.20.0) | | Update to Microsoft.IdentityModel.Validators 6.14.1, provide `MemoryCacheOptions` for `InMemoryCache` on .NET Framework. 
Nov 1st | [1.19.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.19.0) | | Release with AadIssuerValidator package from Microsoft.IdentityModel and support for authentication handlers outside JwtBearer. 
Oct 6th | [1.19.0-preview](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.19.0-preview) | | Release with MSAL.NET 4.36.0-preview, which has cache improvements.
Oct 5th | [1.18.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.18.0) | | Change RequiredScope to be based on policies and bug fixes.
Sept 20th | [1.17.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.17.0) | | Publish Microsoft.Identity.Web.TokenCache and Microsoft.Identity.Web.Certificate for ASP.NET Framework and .NET Core apps. See [package dependencies](https://github.com/AzureAD/microsoft-identity-web/wiki/NuGet-package-references) for more info.
Sept 6th | [1.16.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.16.1) | | Bug fixes
Aug 18th | [1.16.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.16.0) | | DisableL1Cache option, OIDC provider DisplayName, bug fixes
July 30th | [1.15.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.15.2) | | Bug fixes
July 26th | [1.15.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.15.0) | | encryption strategy for the Distributed token cache, delegating handler for token acquisition
July 15th | [1.14.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.14.1) | | Bug fixes, stress improvement in daemon apps
June 23rd | [1.14.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.14.0) | | [Improve cache extensions for net framework](https://github.com/AzureAD/microsoft-identity-web/wiki/asp-net#token-cache-serialization-for-msalnet), [support long running process with OBO](https://github.com/AzureAD/microsoft-identity-web/wiki/get-token-in-event-handler), include backup authentication system routing hint on calls to AAD.
June 15th | [1.13.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.13.1) | | Fix regression from 1.12 with `LegacyCacheCompatibilityEnabled`.
June 11th | [1.13.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.13.0) | | 
June 2nd | [1.12.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.12.0) | | 
May  | [1.11.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.11.0)  | | Support for multiple authentication schemes.
May 17th | [1.10.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.10.0) | | Help rotating client certificates (especially when the certificate description points to KeyVault).
May 4th 2021 | [1.9.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.9.2) | | Support for PKCE + bug fixes.
April 14th 2021 | [1.9.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.9.1) | | Bug fixes and work-arounding a breaking change in a dependency.
April 12th 2021 | [1.9.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.9.0) | [blog post](1.9.0) | Perf improvements, support for NET Framework 4.6.2, support for Regional STS, Azure SDKs, client capabilities.
March 23th 2021 | [1.8.2](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.8.2) | | Update to MSAL 4.28.1.
March 16th 2021 | [1.8.1](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.8.1) | | Bug fix for refreshing the L2 cache when an cached item is found in the L1 cache.
March 10th 2021 | [1.8.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.8.0) | | Provides a more performant L1/L2 token cache, exposes options for L1 cache, improved L2 cache failure scenarios, supports assigned managed identity for certificate loading.
Feb 27th 2021 | [1.7.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.7.0) | | Release of [msidentity-app-sync tool](https://github.com/AzureAD/microsoft-identity-web/blob/master/tools/app-provisioning-tool/README.md), disable ADAL cache lookup by default, X509KeyStorageFlags can be specified, remove obsolete attribute from `ValidateUserScopesAndAppRoles`.
Feb 12th 2021 | [1.6.0](https://github.com/AzureAD/microsoft-identity-web/milestone/33?closed=1) | [blog post](1.6.0) | Simplification of the API, support for decrypt certificate rotation, support and project templates for Azure functions and gRPC services, performance improvement of GetTokenForApp, and update to MSAL.NET 4.26.0
Jan 21th 2021 | [1.5.1](https://github.com/AzureAD/microsoft-identity-web/milestone/32?closed=1) | | Update to the latest version of MSAL .NET (4.25), Microsoft Graph (3.22) and Microsoft Graph Beta (0.36.0-preview)
Jan 20th 2021 | [1.5.0](https://github.com/AzureAD/microsoft-identity-web/milestone/30?closed=1) | | See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.5.0) for details. Support for Azure functions and gRPC. Update of the project templates (adding gRPC and use b2clogin.com). 
Dec 15th 2020 | [1.4.1](https://github.com/AzureAD/microsoft-identity-web/milestone/28) | | See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.4.1) for details. MSAL.NET logs are now surfaced. See [Logging](https://github.com/AzureAD/microsoft-identity-web/wiki/Logging)
Dec 9th 2020 | [1.4.0](https://github.com/AzureAD/microsoft-identity-web/milestone/27) | | See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.4.0) for details. See [Minimal support for ASP.NET](https://github.com/AzureAD/microsoft-identity-web/wiki/asp-net)
Nov 11th 2020 | [1.3.0](https://github.com/AzureAD/microsoft-identity-web/milestone/26) | | See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/1.3.0) for details.
Oct 23rd 2020 | [1.2.0](https://github.com/AzureAD/microsoft-identity-web/milestone/25) | [1.2.0 article](1.2.0) | Scopes and app-permissions for Microsoft Graph, Comfort methods for IDownstreamAPI, Support for App Services Authentication, Support for Ajax calls in Web APIs, For web APIs protected by ACLS, for back channel proxys, and bug fixes
Oct 8th 2020 | [1.1.0](https://github.com/AzureAD/microsoft-identity-web/milestone/24) | 1.1.0 | Improvement to the blazorwasm hosted template, bug fixes
September 30th 2020 | [1.0.0](https://github.com/AzureAD/microsoft-identity-web/milestone/22) | [1.0.0 (GA)](https://github.com/AzureAD/microsoft-identity-web/wiki/1.0.0) | Features and bug fixes.
September 11th 2020 | [0.4.0-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/20) || See [release notes for details](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.4.0-preview).
August 27th 2020 | [0.3.1-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/21) | | See [release notes for details](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.3.1-preview).
August 25th, 2020 | [0.3.0-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/19) | **0.3.0-preview** | See  https://aka.ms/ms-id-web/0.3.0-preview for specific details. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.3.0-preview) for more info.
August 10th, 2020 | [0.2.3-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/18) | **0.2.3-preview** | ReplyForbiddenWithWwwAuthenticateHeaderAsync has an additional optional HttpResponse parameters. Microsoft.Identity.Web works for .NET 5.0.0-* (including Preview 8). See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.2.3-preview) for details.
August 7th, 2020 | [0.2.2-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/17) | **0.2.2-preview** | AadIssuerValidator exposed publicly (to be used in Azure Functions), MicrosoftIdentityConsentAndConditionalAccessHandler can now take an httpContextAccessor, and exposes BaseUri and User. Bug fixes. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.2.2-preview) for details.
July 24th, 2020 | [0.2.1-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/11) | **0.2.1-preview** | Blazor support and token acquisition stability improvements, Blazor templates support, allow specifying B2C user flow for token acquisition calls. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.2.1-preview) for details.
July 13th, 2020 | [0.2.0-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/9) | [Blog post for 0.2.0-preview](Migrating-from-0.1.x-to-0.2.x) | Simplification, support for .NET 5, validation of roles in Web APIs called from daemons. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.2.0-preview) for details.
June 16th, 2020 | [0.1.5-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/6) | **0.1.5-preview** | Support for client and token decryption certificates, use `System.Text.Json` instead of `Newtonsoft.Json`, add `ForceHttpsRedirectUris` option. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.1.5-preview) for details.
June 1st, 2020 | [0.1.4-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/7) | **0.1.4-preview** | Support token acquisition service as a singleton, fix redirect with an unauthorized account, use `user_info` for guest accounts. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.1.4-preview) for details.
May 15th, 2020 | [0.1.3-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/5) | **0.1.3-preview** | Sign-in without passing in scopes is supported, specify the redirectUri and postLogoutRedirectUri, bug fixes. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.1.3-preview) for details.
May 7th, 2020 | [0.1.2-preview](https://github.com/AzureAD/microsoft-identity-web/milestone/3?closed=1) | **0.1.2-preview** | Performance improvements (HttpClientFactory, issuer cache, better error message when the client secret is missing) and bug fixes. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.1.2-preview) for details.
April 22th, 2020 | [0.1.1-preview](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.1.1-preview) | **0.1.1-preview** | Surface `ClaimsConstants` class and bug fixes. See [release notes](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.1.1-preview) for details.
April 13th, 2020 | [0.1.0-preview](https://github.com/AzureAD/microsoft-identity-web/releases/tag/0.1.0-preview) | [Documentation](https://github.com/AzureAD/microsoft-identity-web/wiki) | First preview NuGet package.

For previous, or intermediate releases, see [releases](https://github.com/AzureAD/microsoft-identity-web/releases). See also [Semantic versioning - API change management](Semantic-versioning-and-API-management) to understand changes in Microsoft Identity Web public API, and [Microsoft Identity Web Release Cadence](Release-Cadence) to understand when Microsoft Identity Web is released.

## Samples

### Web App Samples

To see Microsoft Identity Web in action, or learn how to sign-in users with a web app and call a protected web API, use this incremental tutorial on ASP .NET Core web apps which signs-in users (including in your org, many orgs, orgs + personal accounts, sovereign clouds) and calls web APIs (including Microsoft Graph), while leveraging Microsoft Identity Web. [See the incremental tutorial.](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2)

- [Web app which signs in users](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/1-WebApp-OIDC)
- [Web app which signs in users and calls Graph](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/2-WebApp-graph-user)
- [Web app which signs in users and calls multiple web APIs](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/3-WebApp-multi-APIs)
- See the [incremental tutorial](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2) for even more samples, including B2C.

### Web API Samples

To secure web APIs and call downstream web APIs, use this [ASP .NET Core incremental tutorial](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2).

- [Protected web API](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/tree/master/1.%20Desktop%20app%20calls%20Web%20API)
- [Web API calling downstream web API](https://github.com/Azure-Samples/active-directory-dotnet-native-aspnetcore-v2/tree/master/2.%20Web%20API%20now%20calls%20Microsoft%20Graph)
- [Web API called by a Daemon Application](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/tree/master/2-Call-OwnApi)
