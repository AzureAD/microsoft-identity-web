3.2.2
=========
- Updated to Microsoft.IdentityModel.* 8.1.2

3.2.1
=========
- Updated to Microsoft.IdentityModel.* 8.1.1

3.2.0
=========
- Updated to Microsoft.Identity.Abstractions 7.1.0
- Updated to Microsoft.IdentityModel.* 8.1.0
- Updated to Microsoft.Identity.Client 4.64.1
 
### New features
- In .NET 8 and above, `IDownstreamApi` overloads take a `JsonTypeInfo<T>` parameter to enable source generated JSON deserialization. See issue [#2930](https://github.com/AzureAD/microsoft-identity-web/issues/2930) for details.

### Bug fixes:
- Azure region is used while creating application keys when the TokenAcquisition service caches application objects, and the TokenAcquirerFactory caches TokenAcquirer. See [#3002](https://github.com/AzureAD/microsoft-identity-web/pull/3002) for details.
- Improved error messages for FIC. See issue [#3000](https://github.com/AzureAD/microsoft-identity-web/issues/3000) for details.

### Fundamentals:
- Improved test coverage for `GetCacheKey`. See PR [#3020](https://github.com/AzureAD/microsoft-identity-web/pull/3020) for details.
- Update to .NET 9-RC1. See issue [#3025](https://github.com/AzureAD/microsoft-identity-web/issues/3025) for details.
- Fix static analysis warnings. See PR [#3024](https://github.com/AzureAD/microsoft-identity-web/pull/3024) for details.

3.1.0
=========
- Updated to Microsoft.IdentityModel.* 8.0.2

### Security improvement:
- Id Web now uses `CaseSensitiveClaimsIdentity` by default and provides AppContextSwitches to fallback to using `ClaimsIdentity`. This means that when you loopup claims with FindFirst(), FindAll() and HasClaim(), you need to provide the right casing for the claim. See PR [#2977](https://github.com/AzureAD/microsoft-identity-web/pull/2977) for details.

### Bug fixes:
- For SN/I scenarios, Id Web's `GetTokenAcquirer` now sets `SendX5C` in particular protocols. See issue [#2887](https://github.com/AzureAD/microsoft-identity-web/issues/2887) for details.
- Fix for Instance/Tenant parsing for V2 authority (affected one Entra External IDs scenario). See PR [#2954](https://github.com/AzureAD/microsoft-identity-web/issues/2954) for details.
- Fix regex that threw a format exception: `The input string " was not in a correct format` when enabling *same-site cookie compatibility* with userAgent: "Dalvik/2.1.0 (Linux; U; Android 12; Chromecast Build/STTE.230319.008.H1). See issue [#2879](https://github.com/AzureAD/microsoft-identity-web/issues/2879) for details.
- Microsoft.Identity.Web 3.1.0 now has an upper bound set on its dependency on Microsoft.Identity.Abstractions to version 7x to avoid referencing Microsoft.Identity.Abstractions 8.0.0, which has an interface breaking change, not yet implemented in Microsoft.Identity.Web. See PR [#2962](https://github.com/AzureAD/microsoft-identity-web/pull/2962) for details.
  

### Fundamentals:
- Fix flakey tests: [#2972](https://github.com/AzureAD/microsoft-identity-web/pull/2972), [#2984](https://github.com/AzureAD/microsoft-identity-web/pull/2984), [#2982](https://github.com/AzureAD/microsoft-identity-web/issues/2982), 
- Update to `AzureKeyVault@2` in AzureDevOps, [#2981](https://github.com/AzureAD/microsoft-identity-web/pull/2981).
- Update to .NET 9-preview7, [#2980](https://github.com/AzureAD/microsoft-identity-web/pull/2980) and [#2991](https://github.com/AzureAD/microsoft-identity-web/pull/2991).
- It's now possible to build a specific version of Microsoft.Identity.Web based on specific versions of Microsoft.IdentityModel and Microsoft.Identity.Abstractions by specifying build variables on the dotnet pack command (MicrosoftIdentityModelVersion, MicrosoftIdentityAbstractionsVersions, and MicrosoftIdentityWebVersion): [#2974](https://github.com/AzureAD/microsoft-identity-web/pull/2974), [#2990](https://github.com/AzureAD/microsoft-identity-web/pull/2990)

========

**See [rel/v2 branch changelog](https://github.com/AzureAD/microsoft-identity-web/blob/rel/v2/changelog.md#2200) for changes to all 2.x.x versions after 2.18.1.**

**The changes listed in the rel/v2 changelog are also in the 3.x.x versions of Id Web but are not listed here.**

========

3.0.1
=========
- Updated to Microsoft.IdentityModel.* 8.0.1

3.0.0
=========
### CVE package updates
[CVE-2024-30105](https://github.com/advisories/GHSA-hh2w-p6rv-4g7w)
- See PR [#2929](https://github.com/AzureAD/microsoft-identity-web/pull/2929) for details.

- Updated to Microsoft.IdentityModel.* 8.0.0, Microsoft.Identity.Lab API 1.0.2, Microsoft.Identity.Abstractions 6.0.0
- See [rel/v2 changelog](https://github.com/AzureAD/microsoft-identity-web/blob/rel/v2/changelog.md#2200) for full list of added features to 3.0.0.
  
### Fundamentals:
- Update lab cert and lab version. See PR [#2923](https://github.com/AzureAD/microsoft-identity-web/pull/2923) for details.

3.0.0-preview3
=========
- Updated to Microsoft.IdentityModel.* 8.0.0-preview3

3.0.0-preview2
=========
- Updated MSAL .Net to 4.61.3
- Updated Azure.Identity to 1.11.4

### New features:
- Change GetSignedAssertion public API. See issue [#2853](https://github.com/AzureAD/microsoft-identity-web/issues/2853) for details.
- Update to latest .NET 9 preview 4. See issue [#2877](https://github.com/AzureAD/microsoft-identity-web/pull/2877) for details.

### Bug Fixes
- If `Logging:LogLevel:Microsoft.Identity.Web` is assigned to `None`, no default logger is initialized and Microsoft.Identity.Web does not record any logs. See [#2816](https://github.com/AzureAD/microsoft-identity-web/pull/2816) for details. 
- `GraphAuthenticationProvider` checks that the `RequestInformation.URI` is a Graph URI before appending the authorization header, resolving [#2710](https://github.com/AzureAD/microsoft-identity-web/issues/2710). See PR [#2818](https://github.com/AzureAD/microsoft-identity-web/pull/2818) for details.

3.0.0-preview1
=========
### Breaking changes
- Remove netcoreapp3.1 support, see issue [#2262](https://github.com/AzureAD/microsoft-identity-web/issues/2262) for details.
- Remove net5.0 support from Microsoft.Identity.Web.UI, see issue [#2711](https://github.com/AzureAD/microsoft-identity-web/issues/2711) for details.

### New features
- Microsoft.Identity.Web can be conditionally built on `.net9.0-preview`, see issue [#2702](https://github.com/AzureAD/microsoft-identity-web/issues/2702) for details.
- Microsoft.Identity.Web nows processes the `AcceptHeader` and `ContentType` if provided, see issue [#2806](https://github.com/AzureAD/microsoft-identity-web/issues/2806) for details.
- Target Microsoft.IdentityModel 7x in OWIN targets, see issue [#2785](https://github.com/AzureAD/microsoft-identity-web/issues/2785) for details. 

2.18.1
=========
- Updated to Microsoft.IdentityModel.* 7.5.1

### Bug fix
- Fix for FIC due to appending `./default`, see issue [#2796](https://github.com/AzureAD/microsoft-identity-web/issues/2796) for details.

2.18.0
=========
- Updated to Microsoft.Identity.Abstractions 5.3.0
- Updated Azure.Security libraries to 4.6.0

### New features
- Added support for Managed Identity Federated Identity Credential. See issue [#2749](https://github.com/AzureAD/microsoft-identity-web/issues/2749) for details.
- Added support to read a section to register multiple downstream APIs. See issue [#2255](https://github.com/AzureAD/microsoft-identity-web/issues/2255) for details.

### Bug fix
- TokenAcquirer factory is now thread safe and can handle multiple azure regions. See issue [#2765](https://github.com/AzureAD/microsoft-identity-web/issues/2765) for details.

2.17.5
=========
- Updated to MSAL 4.59.1.

2.17.4
=========

### Bug fix

- Fix assertions being removed from `dict` before callback is executed in TokenAcquisition. See issue [#2734](https://github.com/AzureAD/microsoft-identity-web/issues/2734) for details.

2.17.3
=========
- Updated to Microsoft.IdentityModel.* 7.5.0

2.17.2
=========

### New features
- Added support for CIAM custom user domains. You can now use an Open ID connect authority in the "Authority" property of the configuration instead of using "Instance" and "Tenant". See issue [#2690](https://github.com/AzureAD/microsoft-identity-web/issues/2690) for details. 

2.17.1
=========
- Updated to Microsoft.IdentityModel.* 7.4.0

### New features
- DownstreamApi now automatically processes claims challenge from web APIs which are CAE enabled, provided you set "ClientCapablities" : ["cp1"] in the configuation. See issue [#2550](https://github.com/AzureAD/microsoft-identity-web/pull/2550).

### Bug fixes
- Fixes the use of `ServiceDescriptor` for containers which have keyed services present. This can be an issue on .NET 8.0. See issue [#2676](https://github.com/AzureAD/microsoft-identity-web/pull/2676) for details.

### Engineering excellence
- Calls to `ConfidentialClientApplicationBuilderExtension.WithClientCredentials` are fully async. See issue [#2566](https://github.com/AzureAD/microsoft-identity-web/issues/2566) for details.

2.17.0
=========
- Updated to Microsoft.IdentityModel.* 7.3.1 and MSAL.NET 4.59.0

### New features
- Added support for Microsoft.NET.Sdk.Worker. See [Worker calling APIs](https://github.com/AzureAD/microsoft-identity-web/wiki/worker%E2%80%90app%E2%80%90calling%E2%80%90downstream%E2%80%90apis)
- Added support for Managed identity when calling a downstream API on behalf of the app. See [Calling APIs with Managed Identity](https://github.com/AzureAD/microsoft-identity-web/wiki/calling-apis-with-managed-identity) and [PR 2650](https://github.com/AzureAD/microsoft-identity-web/pull/2650). For details see [PR #2645](https://github.com/AzureAD/microsoft-identity-web/issues/2645)

### Bug fixes
- In OWIN applications, GetTokenForUserAsync now respects the ClaimsPrincipal. See issue [#2629](https://github.com/AzureAD/microsoft-identity-web/issues/2629) for details.
- After setting `AddTokenAcquisition(useSingleton:true)` to use token acquisition as a singleton, if you use `.AddMicrosoftGraph` and/or `.AddDownstreamApi` after this call, 
  the GraphServiceClient and IDownstreamApis are now registered as a singleton service. For details see [PR #2645](https://github.com/AzureAD/microsoft-identity-web/issues/2645)
- Added check Against Injection Attacks. For details see [PR 2619](https://github.com/AzureAD/microsoft-identity-web/issues/2619)

### Engineering excellence
- Added a benchmark running on PR merges, available from [https://azuread.github.io/microsoft-identity-web/benchmarks](https://azuread.github.io/microsoft-identity-web/benchmarks) on GitHub pages

2.16.1
=========
- Update Microsoft.Identity.Abstractions 5.1.0 and Microsoft.IdentityModel.* 7.1.2
 
### Bug Fixes
- In OWIN, Id Web now respects the passed in user argument. See issue [#2585](https://github.com/AzureAD/microsoft-identity-web/issues/2585) for details.

2.16.0
=========
- Leverage IdentityModel 7.x on all .NET core frameworks.

2.15.5
=========
- Update to .NET 8 GA
- Update to Microsoft.Graph 5.34.0

### Bug Fixes
- Fixes an issue where users were not able to override ICredentialsLoader. See [#2564](https://github.com/AzureAD/microsoft-identity-web/issues/2564) for details.
- The latest patch version is no longer used in dependencies, as it made builds non-deterministic. See [#2569](https://github.com/AzureAD/microsoft-identity-web/issues/2569) for details.
- Removed dependencies that were no longer needed. See [#2577](https://github.com/AzureAD/microsoft-identity-web/issues/2577) for details.
- Fixes an issue where the build did not look up project names as package dependencies. See [#2579](https://github.com/AzureAD/microsoft-identity-web/issues/2579) for more details.

### Fundamentals
- Enable baseline package validation, see [#2572](https://github.com/AzureAD/microsoft-identity-web/issues/2572) for details.
- Improve trimmability on .NET 8, see [#2574](https://github.com/AzureAD/microsoft-identity-web/issues/2574) for details.

2.15.3
=========
- Update Azure.Identity library to 1.10.2 for CVE-2023-36414.

### Bug Fixes:
- Microsoft.Identity.Web honors the user-provided value for the cache expiry for in-memory cache. See [#2466](https://github.com/AzureAD/microsoft-identity-web/issues/2466) for details.

2.15.2
=========
- For the .NET 8 rc2 target framework, the IdentityModel dependencies have been updated to Identity.Model.*.7.0.3.

### Bug Fixes
- Fixes a regression introduced in 2.15.0 where the OnTokenValidated delegates were no longer chained with an await. See issue[#2513](https://github.com/AzureAD/microsoft-identity-web/issues/2513).

2.15.1
=========
- Updated IdentityModel dependencies to Identity.Model.*.6.33.0 for all target frameworks other than .NET 8 rc1, for which Microsoft,Identity.Web leverages Identity.Model 7.0.2

2.15.0
=========
### New features
- TokenAcquirerFactory now adds support for reading the configuration from environment variables. See issue [#2480](https://github.com/AzureAD/microsoft-identity-web/issues/2480)

#### Experimental API
(to get feedback, could change without bumping-up the major version)
- It's now possible for an application to observe the client certificate selected by Token acquirer from the ClientCredentials properties, and when the certicate is un-selected (because it's rejected by the Identity Provider, as expired, or revoked). See [Observing client certificates](https://github.com/AzureAD/microsoft-identity-web/wiki/Certificates#observing-client-certificates). PR [#2496](https://github.com/AzureAD/microsoft-identity-web/pull/2496)

### Bug Fixes
- Fixes a resiliency issue where the client certificate rotation wasn't always happening (from KeyKeyVault, or certificate store with same distinguished name). See [#2496](https://github.com/AzureAD/microsoft-identity-web/pull/2496) for details.
- In the override of AddMicrosoftIdentityWebApp taking a delegate, the delegate is now called only once (it was called twice causing the TokenValidated event to be called twice as well). Fixes [#2328](https://github.com/AzureAD/microsoft-identity-web/issues/2328)
- Fixes a regression introduced in 2.13.3, causing the configuration to not be read, when using an app builder other than the WindowsAppBuilder with AddMicroosftIdentityWebApp/Api, unless you provided an empty authentication scheme when acquiring a token. Fixes [#2460](https://github.com/AzureAD/microsoft-identity-web/issues/2410), [#2410](https://github.com/AzureAD/microsoft-identity-web/issues/2460), [#2394](https://github.com/AzureAD/microsoft-identity-web/issues/2394)


2.14.0
=========
- Update to Abstractions 5.0.0
- Include new `OpenIdConnect` options from net 8. See PR [#2462](https://github.com/AzureAD/microsoft-identity-web/pull/2462) 

### Bug Fixes
- Chain the OnMessageReceived event. See PR [#2468](https://github.com/AzureAD/microsoft-identity-web/pull/2468)

2.13.4
=========
- Update to IdentityModel 7.0.0-preview5 on .NET 8 and IdentityModel 6.32.3 for the other target frameworks.
- Update to MSAL [4.56.0](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases/tag/4.56.0), which now
  enables the cache synchronization by default
- Support for .NET 8 preview 7. See PR [#2430](https://github.com/AzureAD/microsoft-identity-web/pull/2430)


### Bug fixes
- In Microsoft.Identity.Web.Owin, removed un-needed reference to Microsoft.Aspnet.WebApi.HelpPage. See issue [#2417](https://github.com/AzureAD/microsoft-identity-web/issues/2417)
- Fix to accomodate for breaking change in ASP.NET Core on .NET 8 that the SecurityToken is now a JsonWebToken. See issue [#2420](https://github.com/AzureAD/microsoft-identity-web/issues/2420) 
- Improved the usability of IDownstreamApi by checking all `HttpResponse` for success before returning to the caller, instead of swallowing issues. This is a change of behavior. See issue [#2426](https://github.com/AzureAD/microsoft-identity-web/issues/2426)
- Improvement/Fix of OWIN scenarios, especially the session with B2C: [#2388](https://github.com/AzureAD/microsoft-identity-web/issues/2388)
- Fix an issue with CIAM web APIs and added two CIAM test apps. See PR [#2411](https://github.com/AzureAD/microsoft-identity-web/pull/2411)
- Fix a bug that is now surfaced by the .NET 8 runtime. See issue [#2448](https://github.com/AzureAD/microsoft-identity-web/issues/2448)
- Added a lock while loading credentials. See issue [#2439](https://github.com/AzureAD/microsoft-identity-web/issues/2439)

### Fundamentals
- performance improvements: [#2414](https://github.com/AzureAD/microsoft-identity-web/pull/2414)
- Replaced Selenim with Playwright for more reliable faster UI tests. See issue [#2354](https://github.com/AzureAD/microsoft-identity-web/issues/2354)
- Added MSAL telemetry about the kind of token cache used (L1/L2). See issue [#1900](https://github.com/AzureAD/microsoft-identity-web/issues/1900)
- Resilience improvement: IdWeb now attempts to reload a certificate from its description when AAD returns "certificate revoked" error. See issue [#244](https://github.com/AzureAD/microsoft-identity-web/issues/2444)

2.13.3
=========
- Update to IdentityModel 7.0.0-preview2 on .NET 8.

### New features:
- Support langversion 11, which as fewer allocations compared to 10. See issue [#2351](https://github.com/AzureAD/microsoft-identity-web/issues/2351).
- In AspNET Core 3.1 and Net 5+, Microsoft.Identity.Web now use the DefaultTokenAcquisitionHost (the host for SDK apps) instead of the
Asp.NET Core one, when the service collection was not initialized by ASP.NET Core (that is the `IWebHostEnvironment` is not present in the collection. If you want the ASP.NET Core host, you would need to use the `WebApplication.CreateBuilder().Services` instead
of instantiating a simple service collection.
- In web APIs, `GetAuthenticationResultForUserAsync` tries to find the inbound token from `user.Identity.BootstrapContext` first (if not null), and then from the token acquisition host. This will help for non-asp.NET Core Azure functions for instance.
See issue [#2371](https://github.com/AzureAD/microsoft-identity-web/issues/2371) for details.

2.13.2
=========
### Bug fixes:
- **Fix bug found in usage of AzureAD key issuer validator,** see issue [#2323](https://github.com/AzureAD/microsoft-identity-web/issues/2323).
- **Improved performance in downstreamAPI**, see issue [#2355](https://github.com/AzureAD/microsoft-identity-web/issues/2355) for details.
- **Address duplicate cache entries,** with singleton token acquisition, which was causing much larger cache size than needed. See issue [#2349](https://github.com/AzureAD/microsoft-identity-web/issues/2349).
- **Distributed cache logger now prints correct cache entry size,** see issue [#2348](https://github.com/AzureAD/microsoft-identity-web/issues/2349)

2.13.1
=========
- Update to MSAL 4.55.0

### New Features:
- Support new AzureAD key issuer validator in AddMicrosoftIdentityWebApi by default in Owin. See [#2323](https://github.com/AzureAD/microsoft-identity-web/issues/2323) for details.

- **Microsoft.Identity.Web now supports .NET 8 with conditional compilation**, see [#2309](https://github.com/AzureAD/microsoft-identity-web/issues/2309).
  
2.13.0
=========
- Update to Wilson 6.32.0 and Microsoft.Identity.Abstractions 4.0.0

### New Feature:
Support new AzureAD key issuer validator in AddMicrosoftIdentityWebApi by default. See [#2323](https://github.com/AzureAD/microsoft-identity-web/issues/2323) for details.

2.12.4
==========
- fix for CVE-2023-29331 in `System.Security.Cryptography.Pkcs`

2.12.2
==========
### New Feature:
- **Id Web now supports the MS Graph v5 SDK,** see issue [#2097](https://github.com/AzureAD/microsoft-identity-web/issues/2097) for details.

2.11.1
==========
- Update to MSAL 4.54.1

### Bug Fix:
- **Fix bug with signed assertion for AKS**, see issue [#2252](https://github.com/AzureAD/microsoft-identity-web/pull/2252) for details.

2.11.0
==========
- Update to MSAL 4.54.0

### New Features
- **Id Web now supports [trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)**. See [#2210](https://github.com/AzureAD/microsoft-identity-web/pull/2210)

2.10.0
==========
- Update to Wilson 6.30.0

### New features:
- **Microsoft.Identity.Web now provides more logging in DownstreamAPI**, see [#2148](https://github.com/AzureAD/microsoft-identity-web/issues/2148) for details.
- **OBO support for composite tokens** based assertion and sub_assertion extra query parameters. See issue [#2222](https://github.com/AzureAD/microsoft-identity-web/issues/2222) for details.

### Bug fixes:
- **Fix a regex issue** relating to same site, see [#1811](https://github.com/AzureAD/microsoft-identity-web/issues/1811) for details.
- Bug fixes for CIAM support, see [#2218](https://github.com/AzureAD/microsoft-identity-web/pull/2218) for details.

2.9.0
==========
- Update to Wilson 6.29.0 and MSAL.NET 4.53.0

### Bug Fix:
- **The [ASP.NET Core regression](https://github.com/dotnet/razor/issues/7577) between .NET 5 and 6 with Razor Pages**, is now addressed with Microsoft.Identity.Web.UI targeting .NET 5 until a more permanent solution is found. See issues [#2111](https://github.com/AzureAD/microsoft-identity-web/issues/2111), [#2095](https://github.com/AzureAD/microsoft-identity-web/issues/2095) and [#2183](https://github.com/AzureAD/microsoft-identity-web/issues/2183) for details.

2.8.0
==========
### New features:
- ID Web works with Authority in place of Tenant ID and Domain. See [#2160](https://github.com/AzureAD/microsoft-identity-web/pull/2160)
- ID Web now supports CIAM authorities.
- Abstractions is now updated to version 3.1.0
### Bug fixes:
- Fixed a bug causing ClaimsIdentity.RoleClaimType to always be "roles" when using App Service Authentication. See [#2166](https://github.com/AzureAD/microsoft-identity-web/pull/2166)

2.7.0
==========
### New Feature:
- **`MicrosoftIdentityAppCallsWebApiAuthenticationBuilder` is now available on netstandard2.0**
- **Id Web now supports expressing the cache key used for serializing/deserializing**. See [#2156](https://github.com/AzureAD/microsoft-identity-web/pull/2156)

### Bug Fixes:
- Make `GetClientAssertion` protected.

2.6.1 
========== 
- Update to Wilson 6.27.0 and MSAL.NET 4.51.0

### New Features:
- **`GetClientAssertion` is now public, which enables inheritance of `ClientAssertionProviderBase`**. See [PR](https://github.com/AzureAD/microsoft-identity-web/pull/2112) for details. 
- **Id Web now uses `TryAdd` instead of `Add` in the InMemory and Distributed caches,** this is to not overwrite previously added caches. See [issue](https://github.com/AzureAD/microsoft-identity-web/issues/2090) for details.
- **Id Web now supports MsAuth10ATPop**. See [PR](https://github.com/AzureAD/microsoft-identity-web/pull/2109) for details.

### Bug Fixes:
- **Fix a regression from v1.16.x to v2.5.0 with auth code redemption** when the `ResponseType == "code"`. See issue [#2096](https://github.com/AzureAD/microsoft-identity-web/issues/2096) for details.

### Fundamentals:
- Address compliance and build issues: [#2113](https://github.com/AzureAD/microsoft-identity-web/issues/2113), [#2116](https://github.com/AzureAD/microsoft-identity-web/issues/2116), [#2120](https://github.com/AzureAD/microsoft-identity-web/issues/2120), [#2121](https://github.com/AzureAD/microsoft-identity-web/issues/2121), [#2122](https://github.com/AzureAD/microsoft-identity-web/issues/2122), [#2128](https://github.com/AzureAD/microsoft-identity-web/issues/2128), [#2125](https://github.com/AzureAD/microsoft-identity-web/issues/2125), and [#2137](https://github.com/AzureAD/microsoft-identity-web/issues/2137). 

2.5.0
==========
- Make ClientAssertion public, see [for details](https://github.com/AzureAD/microsoft-identity-web/pull/2079).

2.4.0
==========
- Fix for #2035

2.3.0
==========
- TokenAcquirerFactory.GetDefaultInstance now takes the default configuration section `AzureAd` from where to get the application configuration.

2.2.0
==========
- Re-add netcoreapp3.1 target framework.
- Update to Microsoft.Identity.Abstractions 2.0.1 which includes API change moving from `IDownstreamRestApi` to `IDownstreamApi`.

2.1.0
==========
[Id Web v2.1.0](https://github.com/AzureAD/microsoft-identity-web/wiki/v2.0) brings a varity of new higher-level APIs, including support for .NET Framework (Owin), Daemon scenarios, and the new DownstreamRestApi.

2.0.8-preview
==========
- Update [Microsoft.Identity.Abstractions 1.0.5-preview](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet/releases/tag/1.0.5-preview), which has breaking changes.

2.0.7-preview
==========
- Use ConcurrentDictionary for MergedOptions to resolve [#1957](https://github.com/AzureAD/microsoft-identity-web/issues/1957)

1.25.10
==========
- Merge the PR for #1957.
- Update to Wilson 6.25.1

1.25.9
==========
Use ConcurrentDictionary for MergedOptions to resolve [#1957](https://github.com/AzureAD/microsoft-identity-web/issues/1957)

2.0.6-preview
==========
### New Feature:
- Enable using the TokenAcquireFactory default instance from anywhere in an ASP.NET Core application [#1958](https://github.com/AzureAD/microsoft-identity-web/pull/1958)

### Bug Fixes:
- Fixes a race condition only present in .NET 7 - [#1957](https://github.com/AzureAD/microsoft-identity-web/issues/1957)
- Fix from @rvplauborg to DownstreamWebApiOptions.Clone, which was missing two properties. [#1970](https://github.com/AzureAD/microsoft-identity-web/issues/1970)
- Updates to OWIN and 1P extensibility

1.25.8
==========
### Bug Fix:
- Fix from @rvplauborg to DownstreamWebApiOptions.Clone, which was missing two properties. [#1970](https://github.com/AzureAD/microsoft-identity-web/issues/1970)

1.25.7
==========
### Bug Fix:
- Fixes a race condition only present in .NET 7 - [#1957](https://github.com/AzureAD/microsoft-identity-web/issues/1957)

1.25.6
==========
### Bug Fix:
- Fixes a race condition only present in .NET 7 - [#1957](https://github.com/AzureAD/microsoft-identity-web/issues/1957)

1.25.5
==========
- Update to latest IdentityModel 6.25.0

2.0.4-preview
==========
- Fix Component Governance alerts due to dependent packages. CVE-2022-1941 in Google.Protobuf and CVE-2022-34716 for netcoreapp3.1, cve-2022-29117 for OWIN and cve-2021-24112 for data protection.

### New Features:
- Add support for [private keys in certificates](https://github.com/AzureAD/microsoft-identity-web/pull/1923).

### Bug Fix:
- [#749](https://github.com/AzureAD/microsoft-identity-web/issues/749)

### Perf improvements:
- [Use Throws.cs from R9](https://github.com/AzureAD/microsoft-identity-web/pull/1928), [use high-perf logging for new log messages](https://github.com/AzureAD/microsoft-identity-web/pull/1927), [take suggestions from R9 analyzers](https://github.com/AzureAD/microsoft-identity-web/pull/1924).

2.0.3-preview
==========
### New Features:
- Leverage new [Microsoft.Identity.Abstractions](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet) library, version 1.0.0-preview.

### Bug fixes:
- Bug fixes in the credential loader to use the cached value and make `LoadCredentialsIfNeeded` public.
- Integrate `TokenAcquirerFactory` with ASP NET core.
- Add `ClientSecret` to Owin config #1911.
- Finish the E2E for Owin.

2.0.2-preview
==========
### New Features:
- Support for Proof-of-possession (PoP) as introduced by MSAL.NET 4.47.2.
- Support for .NET 6.

Leverages new [Microsoft.Identity.Abstractions](https://github.com/AzureAD/microsoft-identity-abstractions-for-dotnet) repo.

2.0.0-preview
==========
Detailed released notes [here](https://github.com/AzureAD/microsoft-identity-web/wiki/v2.0).

1.25.4
==========
- Fix Component Governance alerts due to dependent packages. CVE-2022-1941 in Google.Protobuf, CVE-2022-34716 for netcoreapp3.1, CVE-2021-24112 in System.Drawing.Common.

### Bug Fix:
- [#749](https://github.com/AzureAD/microsoft-identity-web/issues/749)

1.25.3
==========
- Update to latest IdentityModel 6.23.1, which has 20% perf improvements.

1.25.2
==========
- Fix Component Governance issues due to dependent packages. CVE-2022-34716 - in DataProtection 5.0.8

1.25.1
==========
### New Features:
**Microsoft.Identity.Web now surfaces the Microsoft.IdentityModel.* logs via the `IIdentityLogger`**. Developers will see an increase in logging, with insight into the request validation logs, especially for web APIs. See issue [#1730](https://github.com/AzureAD/microsoft-identity-web/issues/1730) for details.

### Bug Fixes:
**Regression fix where `AddMicrosoftIdentityUserAuthenticationHandler` needs a scoped service, not a singleton**. See issue [#1757](https://github.com/AzureAD/microsoft-identity-web/issues/1757) for details.

1.25.0
==========
### New Features:
**Microsoft.Identity.Web now supports checking for scopes or app permissions,** via the `RequestedScopeOrAppPermissionAttribute`. See issue [#1641](https://github.com/AzureAD/microsoft-identity-web/issues/1641) for details.

**Extend TokenAcquisitionTokenCredential concept to support tokens as app**. See issue [#1723](https://github.com/AzureAD/microsoft-identity-web/issues/1723) for details.

### Bug Fixes:
**IJwtBearerMiddlewareDiagnostics is now transient and not a singleton**. See issue [#1710](https://github.com/AzureAD/microsoft-identity-web/issues/1710) for details.

**In web API scenario, use the `tid` claim of the incoming assertion, unless overridden**. See issue [#1738](https://github.com/AzureAD/microsoft-identity-web/issues/1738) for details.

1.24.1
==========
### Bug Fixes:
**Microsoft.Identity.Web now returns `TokenValidatedContext.Fail` instead of throwing `UnauthorizedAccessException` in case of missing roles or scopes**, which enables a better developer experience. See issue [#1716](https://github.com/AzureAD/microsoft-identity-web/issues/1716) for details.

1.24.0
==========
Update to Microsoft.IdentityModel 6.17.0.

### New Features:
**Preview only. Support cert-less authentication**. See issues [#1591](https://github.com/AzureAD/microsoft-identity-web/issues/1591) and [#1699](https://github.com/AzureAD/microsoft-identity-web/issues/1699) for details.

**Improved support for inheriting/customizing `MicrosoftIdentity*AuthenticationHandler`**. See issue [#1667](https://github.com/AzureAD/microsoft-identity-web/issues/1667) for details.

### Bug Fixes:
**Fix a regression in ScopeAuthorizationHandler**. See issue [#1707](https://github.com/AzureAD/microsoft-identity-web/issues/1707) for details.

**Fix null ref in merged options and log the AuthenticationScheme that was used**. See issues [#1440](https://github.com/AzureAD/microsoft-identity-web/issues/1440) and [#1443](https://github.com/AzureAD/microsoft-identity-web/issues/1443) for details.

**Fix xml parameter description**. See issue [#1677](https://github.com/AzureAD/microsoft-identity-web/issues/1677) for details.

**Fix reading environment variable in app service auth**. See issue [#1506](https://github.com/AzureAD/microsoft-identity-web/issues/1506) for details.

**Fix error message in DefaultCertficateLoader**. See issue [#1702](https://github.com/AzureAD/microsoft-identity-web/issues/1702) for details.

1.23.1
==========
Update to MSAL.NET 4.42.0.

### Bug Fixes:
**Microsoft.Identity.Web.TokenCache now throws an actionable error message when the L2 cache deserialization fails**, which can happen when the encrypt key of a shared distributed cache are different on different machines. See issues [#1643](https://github.com/AzureAD/microsoft-identity-web/issues/1643) and [MSAL issue 3162](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/3162) for details.

**Fix a null reference when using ITokenAcquisition in a background callback in a web wepp**. See issue [#1656](https://github.com/AzureAD/microsoft-identity-web/issues/1656) for details.

1.23.0
==========
Update to MSAL.NET 4.41.0.

### New Features: 
**Microsoft Identity Web now supports hybrid SPA**. See issue [#1528](https://github.com/AzureAD/microsoft-identity-web/issues/1528) for details.

1.22.3
==========
### Bug Fixes:
**Fix a null reference when the web API is initialized with delegates and called from an event handler, without configuration**. See issues [#1615](https://github.com/AzureAD/microsoft-identity-web/issues/1615) and [#1602](https://github.com/AzureAD/microsoft-identity-web/issues/1602) for details.

1.22.2
==========
Update to Microsoft.IdentityModel 6.15.1.

### Bug Fixes:
**Microsoft.Identity.Web now also checks the `data.RequiredScopesConfigurationKey` when setting the `RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")` attribute**. See issue [#1600](https://github.com/AzureAD/microsoft-identity-web/issues/1600) for details.

**Fix issue around user assigned managed identity when loading KeyVault certificates**. See issue [#1598](https://github.com/AzureAD/microsoft-identity-web/issues/1598) for details.

1.22.1
==========
Update to MSAL.NET 4.40.0.

1.22.0
==========
### New Features:
**Microsoft Identity Web, as a proof of concept, supports certificate-less auth using Managed Service Identity (MSI)**. See issue [#1585](https://github.com/AzureAD/microsoft-identity-web/issues/1585) for details.

**Microsoft Identity Web now allows you set the request headers for the IDownstreamWebAPI**. See issues [#1063](https://github.com/AzureAD/microsoft-identity-web/issues/1063) and [#891](https://github.com/AzureAD/microsoft-identity-web/issues/891) for details.

**Microsoft.Identity.Web.TokenCache exposes a boolean `EnableAsyncL2Write` as part of the `MsalDistributedTokenCacheAdapterOptions`**, which enables you to do async writes (fire and forget) to the L2 cache. See issue [#1047](https://github.com/AzureAD/microsoft-identity-web/issues/1047) and [#1526](https://github.com/AzureAD/microsoft-identity-web/issues/1526) for details.

### Bug Fixes:
**When integrating with the MISE pipeline, client certificates are now taken into account when calling downstream APIs in controllers**. See issue [#1583](https://github.com/AzureAD/microsoft-identity-web/issues/1583) for details.

**When using the L1/L2 cache, the L2 eviction is now based on the token expiration value from MSAL.NET**, similar to what is done with the L1 eviction. See issue [#1566](https://github.com/AzureAD/microsoft-identity-web/issues/1566) for details.

### Compliance:
**Add an SBOM generate to release builds**. See issue [#1546](https://github.com/AzureAD/microsoft-identity-web/issues/1546) for details.

1.21.1
==========
Update to Microsoft.Graph 4.11.0, Microsoft.Graph.Beta 4.22.0-preview, MSAL.NET 4.39.0, Microsoft.IdentityModel 6.15.0.

1.21.0
==========
Update to Microsoft.Graph 4.10.0, Microsoft.Graph.Beta 4.20.0-preview, MSAL.NET 4.38.0

### New Features:
**Microsoft.Identity.Web now supports a long running process in web APIs**, by leveraging new APIs in MSAL.NET 4.38.0. See the [Long running process](https://github.com/AzureAD/microsoft-identity-web/wiki/get-token-in-event-handler) article and issue [#1414](https://github.com/AzureAD/microsoft-identity-web/issues/1414) for details.

### Bug Fixes:
**Honor `TenantId` in `.WithAppOnly()`**. See issue [#1536](https://github.com/AzureAD/microsoft-identity-web/issues/1536) for details.

**Azure Region not prepended to the endpoint**, have fixed a regression in the `MergedOptions`. See issue [#1535](https://github.com/AzureAD/microsoft-identity-web/issues/1535) for details.

**Update `Microsoft.AspNetCore.Authentication.JwtBearer` to 5.0.12**, due to security vulnerability in previous version. See issue [#1532](https://github.com/AzureAD/microsoft-identity-web/issues/1532) for details.

1.20.0
==========
Update to Microsoft.Graph 4.9.0, Microsoft.Graph.Beta 4.19.0-preview, Microsoft.IdentityModel 6.14.1.

### New Features:
**Microsoft.Identity.Web.TokenCache now offers the possiblity of defining MemoryCacheOptions**, such as eviction and size limit options with the InMemoryCache for .NET Framework. See issue [#1521](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/pull/1521) for details.

### Bug Fixes:
**Bug fix in M.IM.Validators when dealing with multiple auth schemes**. See [release notes](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/releases/tag/6.14.1) for details.

1.19.0
==========
Update to Microsoft.Graph 4.8.0, Microsoft.Graph.Beta 4.18.0-preview, Microsoft.IdentityModel 6.14, and MSAL.NET 4.37.0.

### New Features:
**A new assembly, [Microsoft.IdentityModel.Validators](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/tree/dev/src/Microsoft.IdentityModel.Validators), is now leveraged in Microsoft.Identity.Web as the AadIssuerValidator. It provides an issuer validator for the Microsoft identity platform (AAD and AAD B2C)**, working for single and multi-tenant applications and v1 and v2 token types. See [Identity.Model](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/pull/1736) and [#1487](https://github.com/AzureAD/microsoft-identity-web/issues/1487). The `MicrosoftIdentityIssuerValidatorFactory` is still in Microsoft.Identity.Web and leverages this new Identity.Model library

**Microsoft.Identity.Web now supports authentication handlers other than JwtBearer,** and the token acquisition in web API understands a higher level abstraction of [SecurityToken](https://docs.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.securitytoken?view=azure-dotnet), not only `JwtSecurityToken` . See [#1498](https://github.com/AzureAD/microsoft-identity-web/pull/1498).

### Bug Fixes:
**Make `Certificate` in `CertificateDescription.cs` `protected internal`.** See [#1484](https://github.com/AzureAD/microsoft-identity-web/pull/1484).

1.19.0-preview
==========
**This preview release contains a preview version of MSAL.NET, 4.37.0-preview,** which includes token cache improvements. The `.AddMemoryCache` should now be much faster, but the memory is not bounded, nor does it have any eviction policies, so not recommended for use in production if user flows are involved (`GetTokenForUser`). Once MSAL.NET releases 4.37.0, Microsoft.Identity.Web will release an out of preview version as well. 

1.18.0
==========
Update to Microsoft.Graph 4.6.0, Microsoft.Graph.Beta 4.14.0-preview, and MSAL.NET 4.36.2.

### New Features:
**Change RequiredScope to be based on policies and not filters**. This enables new scenarios that do not rely on MVC filters. See issue [#1002](https://github.com/AzureAD/microsoft-identity-web/issues/1002) for details.

### Bug Fixes:
**Allow customizing the UI processing by decoupling the `Microsoft.Identity.Web` and `Microsoft.Identity.Web.Ui` packages**. See issue [#1034](https://github.com/AzureAD/microsoft-identity-web/issues/1034) for details.

**Use `backup authentication system` in docs and comments** instead of CCS. See issue [#1464](https://github.com/AzureAD/microsoft-identity-web/issues/1464) for details.

1.17.0
==========
**Microsoft.Identity.Web now provides two additional NuGet packages: Microsoft.Identity.Web.TokenCache and Microsoft.Identity.Web.Certificate**. These packages are for ASP.NET Framework and .NET Core apps who want to use the token cache serializers and/or the certificate loader, but do not want all the dependencies brought by the full Microsoft.Identity.Web package. If you are on ASP.NET Core, continue to use Microsoft.Identity.Web. See issue [#1431](https://github.com/AzureAD/microsoft-identity-web/issues/1431) for details.

1.16.1
==========
Update to Microsoft.Graph 4.4.0, Microsoft.Graph.Beta 4.11.0-preview, and MSAL.NET 4.36.0.

### Bug Fixes:
**Handle a `SuggestedCacheExpiry` in the past**. See issue [#1419](https://github.com/AzureAD/microsoft-identity-web/issues/1419) for details.

**Fix a `NullReferenceException` when calling `GetTokenForApp` from an anonymous controller**. See issues [#1372](https://github.com/AzureAD/microsoft-identity-web/issues/1372) and [#1348](https://github.com/AzureAD/microsoft-identity-web/issues/1348) for details.

1.16.0
==========
Update to IdentityModel 6.* and Microsoft.Graph 4.2.0 and Microsoft.Graph.Beta 4.7.0-preview.

### New Features:
**The `MsalDistributedTokenCacheAdapterOptions` now expose a boolean `DisableL1Cache`**, which will bypass the InMemory (L1) cache and only use the Distributed cache. See issue (#1388)[https://github.com/AzureAD/microsoft-identity-web/issues/1388] for details.

**When using ASP.NET Individual auth, Microsoft Identity Web provides an overload to define the `DisplayName` of the Identity Provider**. See issue [#808](https://github.com/AzureAD/microsoft-identity-web/issues/808) for details.

### Bug Fixes:
**In .NET Framework, when recreating the CCA each time, the cache is not hit**. Now the ServiceProvider for the InMemory or Distributed cache is not instantiated each time. See issue [#1390](https://github.com/AzureAD/microsoft-identity-web/issues/1390) for details.

**The NonceCookie and CorrelationCookie configurations are now hooked up correctly in Microsoft Identity Web**. See issue [#1262](https://github.com/AzureAD/microsoft-identity-web/issues/1262) for details.

**Fix a transitive `ArgumentException` when adding a preexisting key in the Temp Data**. See PR [#1382](https://github.com/AzureAD/microsoft-identity-web/pull/1382) for details.

**Fix a `KeyNotFoundException` when calling `WithAppOnly()`**. See issue [#1365](https://github.com/AzureAD/microsoft-identity-web/pull/1365) and PR [#1377](https://github.com/AzureAD/microsoft-identity-web/pull/1377/files) for details.

**Remove `context.Success()` in the web API so that further middleware processing can occur**. See issue [#929](https://github.com/AzureAD/microsoft-identity-web/issues/929) for details. 

1.15.2
==========
Update to the latest version of MSAL .NET (4.35.1).

### Bug Fixes:
**Use `CreateAuthorizationHeader()` for GraphClientService requests,** which enables support for other schemes, like PoP. See issue (#1355)[https://github.com/AzureAD/microsoft-identity-web/issues/1355] for details.

**Fix NullReferenceException when customer invokes `OnTokenValidated`**. Microsoft Identity Web now processes the custom `OnTokenValidated` after setting the OBO token. See issue [#1348](https://github.com/AzureAD/microsoft-identity-web/issues/1348) for details.

1.15.1
==========
### Bug Fixes:
**Add `EnableCacheSynchronization` to merged options**. See issue [#1345](https://github.com/AzureAD/microsoft-identity-web/issues/1345).

1.15.0
==========
### New Features:
**Microsoft Identity Web now provides a token cache encryption strategy for the Distributed token cache**. See issue [#1044](https://github.com/AzureAD/microsoft-identity-web/issues/1044) for details.

**Microsoft Identity Web now provides a DelegatingHandler which uses `ITokenAcquisition` to get a token and inject it in the Authorization HTTP headers**. See issue [#1131](https://github.com/AzureAD/microsoft-identity-web/issues/1131) for details.

### Bug Fixes:
**Update XML comment and link**. See issues [#1325](https://github.com/AzureAD/microsoft-identity-web/issues/1325) and [#1322](https://github.com/AzureAD/microsoft-identity-web/issues/1322).

**Update the backup authentication system routing implementation to remove technical debt**. See issue [#1303](https://github.com/AzureAD/microsoft-identity-web/issues/1303).

1.14.1
==========
### New Features:
**Use the `SuggestedCacheKeyExpiry` provided in MSAL.NET 4.34.0 to optimize cache eviction in the app token cache**. See issue [#1304](https://github.com/AzureAD/microsoft-identity-web/issues/1304) for details.

### Bug Fixes:
**Fix an issue with issuer validation with v1 tokens using `/organizations`**. See issues [#1310](https://github.com/AzureAD/microsoft-identity-web/issues/1310) and [#1290](https://github.com/AzureAD/microsoft-identity-web/issues/1290).

**Deterministic builds are now enabled on Azure DevOps**. See issue [#1308](https://github.com/AzureAD/microsoft-identity-web/issues/1308).

**Fix `MergedOption` to merge the scopes**. See issue [#1296](https://github.com/AzureAD/microsoft-identity-web/issues/1296).

1.14.0
==========
### New Features:
**Microsoft Identity Web now provides a more simplified developer experience with the MSAL.NET token cache**, available for ASP.NET, .NET Core, or .NET Framework. See issue [#1277](https://github.com/AzureAD/microsoft-identity-web/issues/1277) for details.

**Microsoft Identity Web supports, out of the box, the AAD backup authentication system which operates as an AAD backup**, by sending a routing hint to the /authorize and /token endpoints. See issue [#1146](https://github.com/AzureAD/microsoft-identity-web/issues/1146) for details.

### Bug Fixes:
**Fix isue regarding specifying multiple decryption certificates**. See issue [#1243](https://github.com/AzureAD/microsoft-identity-web/issues/1243) for details.

1.13.1
==========
### Bug Fixes:
**Fixes a regression that was introduced with the multi-scheme work** where the `LegacyCacheCompatibilityEnabled` value was taken from the `ConfidentialClientApplicationOptions` (default true), instead of the `MicrosoftIdentityOptions` (default false). See issue [#1268](https://github.com/AzureAD/microsoft-identity-web/issues/1268) for details.

1.13.0
==========
### New Features:
**Microsoft Identity Web now supports the CancellationToken**, in the Distributed and Session cache adapters and in the `TokenAcquisitionOptions` for the calls to MSAL.NET. See issue [#1239](https://github.com/AzureAD/microsoft-identity-web/issues/1239) for details.

### Bug Fixes:
**The order of the LogLevel in TokenAcquisition did not correctly honor the nested log settings**. See issue [#1250](https://github.com/AzureAD/microsoft-identity-web/issues/1250) for details.

**Fix a bug with certificate rotation and not pass a null certificate value to Microsoft.IdentityModel**. See issue [#1243](https://github.com/AzureAD/microsoft-identity-web/issues/1243) for details.

**When using EasyAuth, fix case insensitivity when specifying the default provider**. See issue [#1163](https://github.com/AzureAD/microsoft-identity-web/issues/1163) for details.

**EasyAuth took a breaking change by not adding the logout path environment variable**, the logout error with EasyAuth v2 is fixed. See issue [#1234](https://github.com/AzureAD/microsoft-identity-web/issues/1234) for details.

**Microsoft Identity Web now uses the `/.well-known/openid-configuration` endpoint to determine the issuer values**. Now the different clouds work as well. See issue [#1167](https://github.com/AzureAD/microsoft-identity-web/issues/1167) for details.

**A lock in the response stream caused an exception when copying the content to a stream**. See issue [#1153](https://github.com/AzureAD/microsoft-identity-web/issues/1153) for details.

1.12.0
==========
### Bug Fixes:
**Fix issue with `RequiredScope` attribute on the Controller** when used with `RequiredScopesConfigurationKey `. See issues [#1223](https://github.com/AzureAD/microsoft-identity-web/issues/1223), [#1197](https://github.com/AzureAD/microsoft-identity-web/issues/1197), and [#1036](https://github.com/AzureAD/microsoft-identity-web/issues/1036).

**Fix `response_type` in `MergedOptions`**. Regression from 1.10 version. See [#1215](https://github.com/AzureAD/microsoft-identity-web/issues/1215) for details.

**Fix `RoleClaimType` when set as part of the `MicrosoftIdentityOptions`**. Regression from 1.10 version. See [#1218](https://github.com/AzureAD/microsoft-identity-web/issues/1218) for details.

**Microsoft Identity Web UI now displays a better error message when run in a Production environment to assist with debugging**. See issue [#1213](https://github.com/AzureAD/microsoft-identity-web/issues/1213) for details.

**Microsoft Identity Web UI now honors a local redirect URI after sign-in**. This is if you want to redirect the user to a specific page within the add. See issue [#760](https://github.com/AzureAD/microsoft-identity-web/issues/760) for details.

**Fix public API spelling of `CertificateDescription.FromStoreWithThumbprint`**. See issue [#791](https://github.com/AzureAD/microsoft-identity-web/issues/791) for details.

1.11.0
==========
### New Features:
**Microsoft Identity Web now supports multiple authentication schemes**. This means, you can have several authentication schemes in the same ASP.NET Core app. Such as two Azure AD web apps, or an Azure AD app and an Azure AD B2C app, or a web app and a web API. Basically mixing authentication schemes in the same ASP.NET Core app. See the [wiki for details and code samples](https://github.com/AzureAD/microsoft-identity-web/wiki/Multiple-Authentication-Schemes) and related issues: [#549](https://github.com/AzureAD/microsoft-identity-web/issues/549), [#429](https://github.com/AzureAD/microsoft-identity-web/issues/429), [#958](https://github.com/AzureAD/microsoft-identity-web/issues/958), [#1126](https://github.com/AzureAD/microsoft-identity-web/issues/1126), [#971](https://github.com/AzureAD/microsoft-identity-web/issues/971), [#173](https://github.com/AzureAD/microsoft-identity-web/issues/173), [#955](https://github.com/AzureAD/microsoft-identity-web/issues/955), and [#1127](https://github.com/AzureAD/microsoft-identity-web/issues/1127).

### Fundamentals:

**Microsoft Identity Web provides more logging regarding the time spent in the MSAL.NET cache**. See [logging](https://github.com/AzureAD/microsoft-identity-web/wiki/Logging) for information on setting up the logs, and use debug or trace to access the cache specific MSAL.NET logs. 

1.10.0
==========
### New Features:
**Microsoft Identity Web now supports certificate rotation with the Azure KeyVault**. See issue [#956](https://github.com/AzureAD/microsoft-identity-web/issues/956) for details.

1.9.2
==========
### New Features:
**Microsoft Identity Web now includes the Proof Key for Code Exchange (PKCE) on the Authorization Code Grant to minimize authorization code interception attacks**. See issue [#470](https://github.com/AzureAD/microsoft-identity-web/issues/470) for details.

### Bug Fixes:
**Revert fix for breaking change introduced in Microsoft.IdentityModel.* version="6.9"**, which was fixed in v.6.10. See issue [#1140](https://github.com/AzureAD/microsoft-identity-web/issues/1140) for details.

**Standardize the value for `"Domain"` in `appsettings.json` of the templates**. See issue [#1148](https://github.com/AzureAD/microsoft-identity-web/issues/1148) for details.

**Enable workaround to fix regression in App Services authentication due to case sensitivity**. See issue [#1163](https://github.com/AzureAD/microsoft-identity-web/issues/1163) for details.

1.9.1
==========
### Bug Fixes:
**Microsoft.IdentityModel.* version="6.9" introduced a breaking change in the mapping of the User.Identity.Name claim**. Microsoft.Identity.Web 1.9, started leveraging Microsoft.IdentityModel 6.10 to improve resiliency. With this breaking change Microsoft Identity Web 1.9.1 has a temporary workaround in place until a new Microsoft.IdentityModel version is released with a fix. See issues [#1136](https://github.com/AzureAD/microsoft-identity-web/issues/1136) and [#1140](https://github.com/AzureAD/microsoft-identity-web/issues/1140) for details.

**Fix obsolete attribute and error message on `ReplyForbiddenWithWwwAuthenticateHeaderAsync`**. See issue [#1137](https://github.com/AzureAD/microsoft-identity-web/issues/1137) for details.

### Documentation:
**Fix Stackoverflow tags in ReadMe**. See issue [#1128](https://github.com/AzureAD/microsoft-identity-web/issues/1128).

1.9.0
==========
### New Features:
**Microsoft Identity Web now exposes a token provider that the Azure SDKs can use**. See [PR](https://github.com/AzureAD/microsoft-identity-web/pull/542) for details.

**Microsoft Identity Web now supports .NET Framework 4.6.2**. See issue [#1086](https://github.com/AzureAD/microsoft-identity-web/issues/1086).

**Microsoft Identity Web supports calls for regional STS for 1st party only**, this is due to MSAL.NET release [4.29](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/releases/tag/4.29.0), and `AzureRegion` is available via the `ConfidentialClientApplicationOptions`.

### Bug Fixes:
**Microsoft Identity Web now locks on the HttpContext, to better handle multi-threaded applications**. See issue [#1097](https://github.com/AzureAD/microsoft-identity-web/issues/1097) and [PR](https://github.com/AzureAD/microsoft-identity-web/pull/1082) and [PR](https://github.com/AzureAD/microsoft-identity-web/pull/1099).

### Fundamentals:
**Microsoft Identity Web now implements `LoggerMessage` for high performance logging**. See issue [#1105](https://github.com/AzureAD/microsoft-identity-web/issues/1105) for details.

**Performance improvements**. See PRs [#1089](https://github.com/AzureAD/microsoft-identity-web/pull/1089), [#1098](https://github.com/AzureAD/microsoft-identity-web/pull/1098), [#1092](https://github.com/AzureAD/microsoft-identity-web/pull/1092), and [#1085](https://github.com/AzureAD/microsoft-identity-web/pull/1085).

### Documentation:
**Documentation updated to show how to use `ClientCapabilities`**. See issue [#1071](https://github.com/AzureAD/microsoft-identity-web/issues/1071) and also the [wiki]( https://github.com/AzureAD/microsoft-identity-web/wiki/client-capabilities).

**Clear documentation on what is available in Microsoft Identity Web and when to use MSAL.NET, Microsoft Identity Web, or both**. See issue [#1057](https://github.com/AzureAD/microsoft-identity-web/issues/1057) and [Is MSAL.NET right for me?](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Is-MSAL.NET-right-for-me%3F).

1.8.2
==========
Update to latest version of MSAL.NET 4.28.1.

1.8.1
==========
### Bug Fixes:
**With the L1/L2 cache updates in 1.8.0, if a cache item is found in the L1 cache, the L2 cache needs to be refreshed**. See issue [#1061](https://github.com/AzureAD/microsoft-identity-web/issues/1061) for details.

1.8.0
==========
### New Features:
**Microsoft Identity Web now provides a more sophisticated and performant L1/L2 (In Memory and Distributed) token cache**. See issue [#957](https://github.com/AzureAD/microsoft-identity-web/issues/957) for details.

**Related to the L1/L2 cache improvements, developers can determine how to proceed when the L2 (Distributed) cache fails, ex. the L2 cache is off-line**. See issue [#1042](https://github.com/AzureAD/microsoft-identity-web/issues/1042) for details.

**Related to the L1/L2 cache improvements, the `MemoryCacheOptions` are now exposed in the `MsalDistributedTokenCacheAdapterOptions` so developers can have control over the L1 (In Memory) cache, such as cache size**. See issue [#1048](https://github.com/AzureAD/microsoft-identity-web/issues/1048) for details.

**Microsoft Identity Web supports user assigned managed identity for certificate loading**. See issue [#1007](https://github.com/AzureAD/microsoft-identity-web/issues/1007) for details.

1.7.0
==========
### New Features:
**msidentity-app-sync is a command line tool that creates Microsoft identity platform applications in a tenant (AAD or B2C)** and updates the configuration code of your ASP.NET Core applications (mvc, webapp, blazorwasm, blazorwasm hosted, blazorserver). The tool can also be used to update code from an existing AAD/AAD B2C application. See https://aka.ms/msidentity-app-sync for details and [additional information on the experience in Visual Studio 16.9](https://github.com/AzureAD/microsoft-identity-web/blob/master/tools/app-provisioning-tool/vs2019-16.9-how-to-use.md). Get the tool via the [NuGet package](https://www.nuget.org/packages/msidentity-app-sync/). See issue [#954](https://github.com/AzureAD/microsoft-identity-web/issues/954), and [977](https://github.com/AzureAD/microsoft-identity-web/issues/977).

**Microsoft Identity Web now disables the ADAL cache lookup by default when calling into MSAL .NET**. If you have ADAL apps which share a cache with MSAL apps, you would want to set `LegacyCacheCompatibilityEnabled = true` in `appsettings.json`. Otherwise, there is a performance improvement when bypassing the ADAL cache lookup. See issue [#961](https://github.com/AzureAD/microsoft-identity-web/issues/961) for details.

**It's now possible to specify the X509KeyStorageFlags in the certificate description (both in the config file, or programmatically)**. This way if you want to use other storage flags than the default, it is possible.

### Bug Fixes:
**Remove obsolete attribute from `ValidateUserScopesAndAppRoles`**. See issue [#963](https://github.com/AzureAD/microsoft-identity-web/issues/963) and [#995](https://github.com/AzureAD/microsoft-identity-web/issues/995) for details.

1.6.0
==========
See [blog post](https://github.com/AzureAD/microsoft-identity-web/wiki/1.6.0) for details.

### New Features:
**Microsoft Identity Web templates now include a project template for Azure Functions**. See issue [#899](https://github.com/AzureAD/microsoft-identity-web/issues/899) for details.

**gRPC templates now include calling graph and downstream APIs**. See issue [#900](https://github.com/AzureAD/microsoft-identity-web/issues/900) for details.

**Microsoft Identity Web now exposes an AuthorizationFilter attribute to express accepted scopes on controllers, actions, or pages**. See issue [#849](https://github.com/AzureAD/microsoft-identity-web/issues/849) for details.

**When using the delegate override of `.EnableTokenAcquisitionToCallDownstreamApi`, you don't need to repeat the properties present in the Microsoft Identity Options ex. Instance, TenantId, ClientId, etc...**. See issue [#742](https://github.com/AzureAD/microsoft-identity-web/issues/742) for details.

**Microsoft Identity Web now exposes the `DefaultCertificateLoader`, which would be used when loading a certificate from a daemon application, or an ASP NET application, using MSAL .NET directly**. See issue [#952](https://github.com/AzureAD/microsoft-identity-web/issues/952) for details.

### Bug Fixes:
**Microsoft Identity Web now supports token decryption certificates rotation**. See issue [#905](https://github.com/AzureAD/microsoft-identity-web/issues/905) for details.

**Microsoft Identity Web now allows the AuthorizeForScopeAttribute to specify an alternate AuthenticationScheme**. See issue [#870](https://github.com/AzureAD/microsoft-identity-web/issues/870) for details.

1.5.1
==========
Update to the latest version of MSAL .NET (4.25), Microsoft Graph (3.22) and Microsoft Graph Beta (0.36.0-preview).

1.5.0
==========
### New Features:
**Microsoft Identity Web templates now include a project template for gRPC**. See issue [#628](https://github.com/AzureAD/microsoft-identity-web/issues/628) for details.

**Microsoft Identity Web now helps writing Azure Functions protected with Azure AD or Azure AD B2C**. See issue [#878](https://github.com/AzureAD/microsoft-identity-web/issues/878).

**The Microsoft Identity Web B2C templates now use the recommended `.b2clogin.com`** instead of `login.microsoftonline.com` by default. See issue [#792](https://github.com/AzureAD/microsoft-identity-web/issues/792) for details.

### Bug Fixes:
**In a Blazor server application, when the client app requests consent for the web API, the call would result in an infinite loop**. The consent screen is now correctly displayed. See issue [#847](https://github.com/AzureAD/microsoft-identity-web/issues/847) for details.

1.4.1
==========
### New Features:
**Microsoft Identity Web now leverages the logs available in MSAL .NET**. See the [wiki](https://github.com/AzureAD/microsoft-identity-web/wiki/Logging) for information on setting up the logs and how to enable Pii. See issue [#821](https://github.com/AzureAD/microsoft-identity-web/issues/821) for details.

1.4.0
==========
### New Features: 
**Starting in MSAL .NET 4.24, the `.WithForceRefresh()` parameter is passed to the on-behalf-of call**. Microsoft Identity Web now incudes it in the on-behalf-of call. It is false by default, as part of the `TokenAcquisitionOptions`. See issue [#811](https://github.com/AzureAD/microsoft-identity-web/issues/811) for details.

**Microsoft Identity Web now exposes the generic consent handler in Razor pages and MVC controllers in addition to Blazor pages (by registering it on a `IServiceCollection`)**. See issue [#805](https://github.com/AzureAD/microsoft-identity-web/issues/805) for details.

### Bug Fixes:
**Microsoft Identity Web was validating the issuer even when `ValidateIssuer` was set to false**. This is now fixed. See issue [#797](https://github.com/AzureAD/microsoft-identity-web/issues/797) for details.

**Microsoft Identity Web now uses the redirect URI if you provide it as part of the `ConfidentialClientApplicationOptions`**. See issue [#784](https://github.com/AzureAD/microsoft-identity-web/issues/784) for details.

**Microsoft Identity Web provides a better experience for app developers who use the legacy `login.microsoftonline.com/tfp/` authority for B2C applications**. See issue [#143](https://github.com/AzureAD/microsoft-identity-web/issues/143) for details.

**A tenanted authority must be used in the acquire token for app scenario**. If `common` or `organizations` is used, Microsoft Identity Web will throw an actionable exception. See issue [#793](https://github.com/AzureAD/microsoft-identity-web/issues/793) for details.

**The wrong constant values were used for LoginHint and DomainHint**. See issue [798](https://github.com/AzureAD/microsoft-identity-web/issues/798) and [PR](https://github.com/AzureAD/microsoft-identity-web/pull/812) for details.

**Microsoft Identity Web now supports individual auth with AAD external providers**. To enable this, you can now specify a null cookie scheme in `AddMicrosoftIdentityWebApp`. See issue [#133](https://github.com/AzureAD/microsoft-identity-web/issues/133) and issue [#809](https://github.com/AzureAD/microsoft-identity-web/issues/809).

1.3.0
==========
### New Features:
**Microsoft Identity Web now exposes token cache adapters for Memory and IDistributedCache for .NET 4.7.2**, so ASP .NET MVC developers can leverage the serializers. See issue [#741](https://github.com/AzureAD/microsoft-identity-web/issues/741) for details.

### Bug Fixes:
**Microsoft Identity Web now guards against an authority ending with `//`**. See issue [#747](https://github.com/AzureAD/microsoft-identity-web/issues/747) for details.

**During AJAX calls, Microsoft Identity Web ensures the redirect URI is a local redirect URI**. See issue [#746](https://github.com/AzureAD/microsoft-identity-web/issues/746).

**KeyVault flags are now included in the private key path for certificate fetching**. See issue [#762](https://github.com/AzureAD/microsoft-identity-web/issues/762) for details.

1.2.0
==========
### New Features:
**Microsoft Identity Web now supports App Services Authentication with Azure AD**. See https://aka.ms/ms-id-web/AppServicesAuth and issue [#8](https://github.com/AzureAD/microsoft-identity-web/issues/8) for details.

**Microsoft Identity Web now enables the usage of the `GraphServiceClient` to call the Graph APIs with app only permissions**. See https://aka.ms/ms-id-web/microsoftGraph and issue [#654](https://github.com/AzureAD/microsoft-identity-web/issues/654) for details.

**Microsoft Identity Web now supports a variety of generic extension methods for use with the downstream web API calls**. See issue [#537](https://github.com/AzureAD/microsoft-identity-web/issues/537) for details.

**To better support Conditional Access scenarios, `TokenAcquisitionOptions` now have a `Claims` property**. See issue [#677](https://github.com/AzureAD/microsoft-identity-web/issues/677) for details.

**Using AJAX to make calls to a .NET Core application is now possible with Microsoft Identity Web**. See issues [#642](https://github.com/AzureAD/microsoft-identity-web/issues/642) and [#603](https://github.com/AzureAD/microsoft-identity-web/issues/603).

**In order to enable web APIs called by daemon applications to handle tokens without a roles claim, Microsoft Identity Web now exposes a boolean property in `MicrosoftIdentityOptions`**. See issue [#707](https://github.com/AzureAD/microsoft-identity-web/issues/707) for details.

### Bug Fixes:
**The Microsoft.Identity.Web.UI DLL now includes strong name validation**. See issue [#682](https://github.com/AzureAD/microsoft-identity-web/issues/682).

**The `AadIssuerValidator` class no longer has a static `ConfigurationManager`, and is instead an injectable singleton**. See issue [#402](https://github.com/AzureAD/microsoft-identity-web/issues/402) for details.

**Microsoft Identity Web would try to add to the authorization header, at times, resulting in a format exception**. Now the existing header is removed and replaced with the current one. See issue [#673](https://github.com/AzureAD/microsoft-identity-web/issues/673) for details.

**In order to enable developers to use a backchannel proxy, Microsoft Identity Web now enables developers to configure the `IHttpClientFactory` to include a name option which will be passed to `CreateClient` via the `AadIssuerValidatorOptions`**. See https://aka.ms/ms-id-web/proxy and issue [#551](https://github.com/AzureAD/microsoft-identity-web/issues/551) for more details.

1.1.0
===========
### New Features:
**When using the InMemory token cache, Microsoft Identity Web enabled developers to `MemoryCacheOption`**, this can improve performance. See issue [#639](https://github.com/AzureAD/microsoft-identity-web/issues/639).

### Bug Fixes:
**The `.Clone()` in TokenValidationParameters has been removed as it is not needed**. See issue [#635](https://github.com/AzureAD/microsoft-identity-web/issues/635) for details.

**The `RequestContent` parameter in DownstreamWebApi is now being used as the `HttpRequestMessage.Content` if available**.See issue [#618](https://github.com/AzureAD/microsoft-identity-web/issues/618).

**Microsoft Identity Web now checks for the tenantId long-claim in AadIssuerValidator.GetTenantIdFromToken**. See issue [#617](https://github.com/AzureAD/microsoft-identity-web/issues/617) for details.

**In the blazorwasm-hosted templates, the Call Graph and Call Downstream Web Api options are now surfaced as separate pages and separate entries in the vertical menu**. See issue [509](https://github.com/AzureAD/microsoft-identity-web/issues/509).

**In `MicrosoftIdentityConsentAndConditionalAccessHandler.HandleException`, the redirect uri could be malformed, containing an extra `/`**. This has been fixed. See issue [#626](https://github.com/AzureAD/microsoft-identity-web/issues/626) for details.

### Fundamentals:
**Microsoft Identity Web has completed initial performance and load testing**. See [wiki article](https://github.com/AzureAD/microsoft-identity-web/wiki/performance) and issue [#88](https://github.com/AzureAD/microsoft-identity-web/issues/88) for details.

**Microsoft Identity Web dependencies are updated to the latest respective versions**. Also the blazorwasm template dependencies have been updated as well. See issues [#641](https://github.com/AzureAD/microsoft-identity-web/issues/641) and [#631](https://github.com/AzureAD/microsoft-identity-web/issues/631) for details.

1.0.0
===========
### New Features:
**Some constant values used in Microsoft Identity Web are available as public constants**. See feature request [#548](https://github.com/AzureAD/microsoft-identity-web/issues/548) for details.

**Microsoft Identity Web now sends basic telemetry data (sku and version) to AAD and AAD B2C**. See issue [#327](https://github.com/AzureAD/microsoft-identity-web/issues/327) for details.

**Implement `TokenAcquisitionOptions` which enable developers to customize the token aquisition integration with MSAL .NET**. Current options available are extra query parameters, force refresh, and correlation id. See issues [#561](https://github.com/AzureAD/microsoft-identity-web/issues/561), [#494](https://github.com/AzureAD/microsoft-identity-web/issues/494), and [#532](https://github.com/AzureAD/microsoft-identity-web/issues/532).

### Bug Fixes:
**Microsoft Identity Web now uses a scoped service for TokenAcquisitionServices when calling Microsoft Graph**. Previously a Singleton was used and this caused an infinite loop in Blazor ser_ver applications, as Blazor requires scoped services. See issues [#573](https://github.com/AzureAD/microsoft-identity-web/issues/573) and [#531](https://github.com/AzureAD/microsoft-identity-web/issues/531) for details.

**Now developers can specify the client secret in the web API scenario either in Microsoft Identity Options or in the Confidential Client Application Options**, previously it had to be set in both. See issue [#536](https://github.com/AzureAD/microsoft-identity-web/issues/536) for details.

**Web apps calling web APIs no longer require a `response_type` of `id_token`, so it no longer needs to be checked in the AAD portal app registration**. See issue [#589](https://github.com/AzureAD/microsoft-identity-web/issues/589).

### Fundamentals:
**Remove obsolete attributes for the 1.0.0 (GA) version**. See issue [#584](https://github.com/AzureAD/microsoft-identity-web/issues/584) for details.

0.4.0-preview
============
### New Features:
**`ITokenAcquisition` now exposes the `AuthenticationResult` for the user from MSAL**. See issue [#543](https://github.com/AzureAD/microsoft-identity-web/issues/543) for details.

**Now, to use Microsoft GraphServiceClient, you need to reference Microsoft.Identity.Web.MicrosoftGraph or Microsoft.Identity.Web.MicrosoftGraphBeta**. See issue [#506](https://github.com/AzureAD/microsoft-identity-web/issues/506) for details.

### Bug Fixes:
**`CallWebApiForUserAsync` handles a successful response better**. See issue [#503](https://github.com/AzureAD/microsoft-identity-web/issues/429) for details.

**Microsoft Identity Web can now handle two schemes in web APIs**. See issues [#429](https://github.com/AzureAD/microsoft-identity-web/issues/429), [#468](https://github.com/AzureAD/microsoft-identity-web/issues/468), and [#474](https://github.com/AzureAD/microsoft-identity-web/issues/474) for details.

### Fundamentals:
**Add integration test coverage for web app and web API scenarios**. Issues [#97](https://github.com/AzureAD/microsoft-identity-web/issues/97), [#95](https://github.com/AzureAD/microsoft-identity-web/issues/95), and [#102](https://github.com/AzureAD/microsoft-identity-web/issues/102).

0.3.1-preview
============
### Bug Fixes
**In B2C web app scenarios, only signing-in users, the password reset and edit profile redirects were not working**. Microsoft Identity Web now only sends the `response_type` of only `idToken` when in the web app scenario. See issue on [password reset](https://github.com/AzureAD/microsoft-identity-web/issues/467) and [edit profile](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/issues/399) for details.

0.3.0-preview
============
### API Breaking Changes:
See https://aka.ms/ms-id-web/0.3.0-preview for more specific details.

Before | After
-- | --
services.AddMicrosoftWebAppAuthentication() | services.AddMicrosoftIdentityWebAppAuthentication()
services.AddAuthentication().AddMicrosoftWebApp() | services.AddAuthentication().AddMicrosoftIdentityWebApp()
services.AddMicrosoftWebApiAuthentication() | services.AddMicrosoftIdentityWebApiAuthentication()
services.AddAuthentication().AddMicrosoftWebApi() | services.AddAuthentication().AddMicrosoftIdentityWebApi()
services.AddAuthentication().AddMicrosoftWebApp().AddMicrosoftWebAppCallsWebApi() | services.AddAuthentication().AddMicrosoftIdentityWebApp().EnableTokenAcquisitionToCallDownstreamApi()
services.AddAuthentication().AddMicrosoftWebApi().AddMicrosoftWebApiCallsWebApi() | services.AddAuthentication().AddMicrosoftIdentityWebApi().EnableTokenAcquisitionToCallDownstreamApi()
services.AddInMemoryTokenCaches() | .EnableTokenAcquisitionToCallDownstreamApi().AddInMemoryTokenCaches()
services.AddDistributedTokenCaches() | .EnableTokenAcquisitionToCallDownstreamApi().AddDistributedTokenCaches()
services.AddSessionTokenCaches() | .EnableTokenAcquisitionToCallDownstreamApi().AddSessionTokenCaches()
services.AddMicrosoftGraph() | .EnableTokenAcquisitionToCallDownstreamApi().AddMicrosoftGraph()
services.AddDownstreamApiService() | .EnableTokenAcquisitionToCallDownstreamApi().AddDownstreamApi()

See issue [#378](https://github.com/AzureAD/microsoft-identity-web/issues/378) and the [wiki](https://github.com/AzureAD/microsoft-identity-web/wiki/0.3.0-preview) for more information on the new API.

**`AddInMemoryTokenCaches` method now accepts an optional `MsalMemoryTokenCacheOptions` delegate parameter**.  See issue for details: [#426](https://github.com/AzureAD/microsoft-identity-web/issues/426).

**`GetAccessTokenForAppAsync` method now accepts an optional `tenant` parameter**, which allows applications authorized in multiple tenants to request tokens.  See issue for details: [#413](https://github.com/AzureAD/microsoft-identity-web/issues/413).


### New Features:
**Microsoft Identity Web now provides methods that simplify calling Microsoft Graph and any downstream APIs**. See [wiki](https://github.com/AzureAD/microsoft-identity-web/wiki/0.3.0-preview) and issues for details: [#403](https://github.com/AzureAD/microsoft-identity-web/issues/403), [#427](https://github.com/AzureAD/microsoft-identity-web/issues/427).

**Project templates, samples, and dev apps were updated to use the new public API**.  See issues for details: [#453](https://github.com/AzureAD/microsoft-identity-web/issues/453), [#418](https://github.com/AzureAD/microsoft-identity-web/issues/418).

### Bug Fixes:
**Previously domain hint was added to the request only if the login hint was present also**. The presence of domain and login hints is now validated separately. See issue for details: [#415](https://github.com/AzureAD/microsoft-identity-web/issues/415).

**Fixed a `NullReferenceException` on `NavigationManager` that occurred on Blazor server with Azure SignalR when using a pre-rendering mode**. See issue for details: [#437](https://github.com/AzureAD/microsoft-identity-web/issues/437).

0.2.3-preview
============
### New features:
**`ReplyForbiddenWithWwwAuthenticateHeaderAsync` method in `ITokenAcquisition` now has an additional optional `HttpResponse` parameter**, which can be provided in cases when the current `HttpContext` is null.  See issue for details: [#414](https://github.com/AzureAD/microsoft-identity-web/issues/414).

**Enable Micorosoft.Identity.Web to work with any version of .NET 5.0** by setting dependencies version to `5.0.0-*` for `JwtBearer` and `OpenIdConnect` dependencies**. See issue for details: [#380](https://github.com/AzureAD/microsoft-identity-web/issues/380).

0.2.2-preview
============
### New features:
**The `AadIssuerValidator` class is now public**. See issue for details: [#332](https://github.com/AzureAD/microsoft-identity-web/issues/332).

### Bug fixes:
**Starting in 0.2.1-preview, a `MicrosoftIdentityWebChallengeUserException` was added, but customers might use the `MsalUiRequiredException`, for instance by the Graph SDK**. See issue for details: [#398](https://github.com/AzureAD/microsoft-identity-web/issues/398).

**In a multi-tenant scenario, when calling a downstream API, Microsoft Identity Web was not returning the token for the specific tenant ID**. The correct token based on the tenant, if specified, is returned. See issues for details: [#344](https://github.com/AzureAD/microsoft-identity-web/issues/344) and [MSAL .NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1965).

**When the scopes provided are invalid, an exception will be thrown in addition to recording a response back to the controller**. This ensures the controller does not continue processing as authentication is not possible. See issue for details: [#389](https://github.com/AzureAD/microsoft-identity-web/issues/389).

**When calling a downstream web API, Microsoft Identity Web now checks the token from the HttpContext instead of doing an acquire token silent call.** This will save on cycles as MSAL .NET already does the necessary cache look up. See issue for details: [#381](https://github.com/AzureAD/microsoft-identity-web/issues/381).

**When validating the application roles, only the first role claim was used, which would result in a failure with multiple roles**. Microsoft Identity Web now uses all the roles and throws an exception if the roles are invalid. See issue for details: [#374](https://github.com/AzureAD/microsoft-identity-web/issues/374).

**A more descriptive exception is thrown when a B2C issuer claim contains `tfp`**. See [wiki](https://github.com/AzureAD/microsoft-identity-web/wiki/Azure-AD-B2C-issuer-claim-support) and issue for details: [#274](https://github.com/AzureAD/microsoft-identity-web/issues/274).

0.2.1-preview
============
### New features:
**Microsoft Identity Web now supports the ComponentsWebAssembly-CSharp project templates from ASP.NET Core**. See issue for details: [#320](https://github.com/AzureAD/microsoft-identity-web/issues/320).

**Microsoft Identity Web now supports the BlazorServerWeb-CSharp project templates from ASP .NET Core**. See issue for details: [#319](https://github.com/AzureAD/microsoft-identity-web/issues/319).

**When using Azure AD B2C, developers can now specify which policy/user flow to use to acquire the token**. Microsoft Identity Web now exposes a user flow parameter, which allows developers to specify the policy/user flow to use with looking for tokens in the cache. See issue for details: [#27](https://github.com/AzureAD/microsoft-identity-web/issues/27).

### Bug fixes:
**Fixes `NullReferenceException` when acquiring a user token because of a null `HttpContext`**. This applies to scenarios like server-side Blazor apps, long-running processes, daemon apps. See issues for details: [#157](https://github.com/AzureAD/microsoft-identity-web/issues/157), [#10](https://github.com/AzureAD/microsoft-identity-web/issues/10), [#38](https://github.com/AzureAD/microsoft-identity-web/issues/38).

**When encountering a challenge from a Blazor page, Microsoft Identity Web could not handle the challenge in the same way it does for an MVC or Razor page**. The challenge from a Blazor page is now handled corrected. See issue for details: [#360](https://github.com/AzureAD/microsoft-identity-web/issues/360).

**When acquiring a token for a user in a server side Blazor app, a null reference exception was encountered because the `CurrentHttpContext` was null**. Microsoft Identity Web now uses a Blazor specific class `AuthenticationStateProvider` to determine the current user. See issue for details: [#157](https://github.com/AzureAD/microsoft-identity-web/issues/157).

**In the templates, `CallWebApi` is an async method and now named accordingly, `CallWebApiAsync`**. See issue for details: [#357](https://github.com/AzureAD/microsoft-identity-web/issues/357).

**Microsoft Identity Web now creates a `HttpRequestMessage` to append the authorization header with the bearer token**. See issue for details: [350](https://github.com/AzureAD/microsoft-identity-web/issues/350).

**In the case of no authentication, the templates no longer support the usage of `--call-graph` and `--call-webapi-url`**. See issue for details: [337](https://github.com/AzureAD/microsoft-identity-web/issues/337).

**Now that Microsoft Identity Web can specify the AAD B2C tokens by user flow, the correct method, `GetAccountAsync()` can be called for confidential clients**. See issue for details: [#295](https://github.com/AzureAD/microsoft-identity-web/issues/295).

### Fundamentals:
**Microsoft Identity Web has error and log messages as constants**. See issue for details: [#261](https://github.com/AzureAD/microsoft-identity-web/issues/261).

0.2.0-preview
============
### API breaking changes:
Before | After
-- | --
services.AddSignIn() | services.AddMicrosoftWebAppAuthentication()
services.AddSignIn() | services.AddAuthentication().AddMicrosoftWebApp()
services.AddProtectedWebApi() | services.AddMicrosoftWebApiAuthentication()
services.AddProtectedWebApi() | services.AddAuthentication().AddMicrosoftWebApi()
.AddWebAppCallsProtectedWebApi() | .AddMicrosoftWebAppCallsWebApi()
.AddProtectedWebApiCallsWebApi() | .AddMicrosoftWebApiCallsWebApi()

- See the [wiki](https://aka.ms/ms-id-web/net5) for migration assistance and more information on the new API.
- Rename `MsalMemoryTokenCacheOptions.SlidingExpiration` to align with ASP.NET Core and use `AbsoluteExpirationRelativeToNow`. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/250).
- Removed the `ForceHttpsRedirectUris`, `RedirectUri`, and `PostLogoutRedirectUri` options from `MicrosoftIdentityOptions`. ASP.NET Core recommends the [following guidance](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-3.1) on working with proxies. See [issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/223).
- Removed the `SingletonTokenAcquisition` property from `MicrosoftIdentityOptions`. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/249).
- Microsoft Identity Web now has an `MsalDistributedTokenCacheAdapterOptions` class inheriting from `DistributedCacheEntryOptions` so the token cache serialization can expose their own options. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/251).

### New Features:
**Microsoft Identity Web implements the C# 8.0 nullable standard**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/15).

**Microsoft Identity Web now validates the app roles for a web API, for example a web API called by a daemon application**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/131).

**Microsoft Identity Web now supports .NET 5.0**, in addition to .NET Core 3.1. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/194).

**The project templates now have an option to generate the call to a downstream web API, or a call to Microsoft Graph**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/196).

**Microsoft Identity Web now has the ability to specify custom cookie options in the `AddMicrosoftWebApp` methods**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/237).

### Bug Fixes:
**When accessing KeyVault, storage flags need to be used, as there is no user profile**. The correct storage flags are now used. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/219).

**Uses the recommended workaround for the clients incompatible with the `SameSite=None` cookie attribute**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/238).

**Fixed a dependency injection anti-pattern when resolving `ITokenAcquisition`**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/240).

**The `TokenValidationParameters` are now cloned before using**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/241).

**Microsoft Identity Web now throws a `SecurityTokenValidationException`**, when there is an invalid audience. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/242).

**Microsoft Identity Web no longer throws an exception if the user sets the custom audiences**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/243).

**Removed multiple calls to `HandleCodeRedemption` in `TokenAcquisition`**. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/246).

**Fixes to the `MsalSessionTokenCacheProvider`**, such as removing the static lock object and removing the session commit. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/252).

**In the `AccountController`, Microsoft Identity Web now uses `IOptions` instead of `IOptionsMonitor`**, for consistency. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/232).

**Microsoft Identity Web no longer calls the `BuildServiceProvider` in the configuration methods** and uses a more appropriate `Configure` overload that provides the required `IServiceProvider` instance. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/234).

**Microsoft Identity Web now uses the `SuggestedCacheKey`** returned in the `TokenCacheNotificationArgs` from MSAL to determine the correct cache key. This enables the removal of several lines of code and the use of the `HttpContext.User`. See issue [235](https://github.com/AzureAD/microsoft-identity-web/issues/235), [248](https://github.com/AzureAD/microsoft-identity-web/issues/248), [273](https://github.com/AzureAD/microsoft-identity-web/issues/273), and [222](https://github.com/AzureAD/microsoft-identity-web/issues/222) for details.

**Microsoft Identity Web now retrieves the `client_info` data directly from the protocol message**. See [issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/245).

0.1.5-preview
============
### New Features:
**Microsoft Identity Web supports certificates.** The developer can now use client and token decryption certificates, which can be retrieved from a variety of sources, like Azure Key Vault, certificate store, a Base54 encoded string, and more. The location of the certificate can be specified in a configuration file or programmatically. See [issue](https://github.com/AzureAD/microsoft-identity-web/issues/165) and [wiki](https://github.com/AzureAD/microsoft-identity-web/wiki/Using-certificates) for more details.

**Microsoft Identity Web now allows specifying if the x5c claim (the public key of the certificate) should be sent to the STS.** Sending the x5c enables easy certificate rollover. To enable this behavior set the `SendX5C` property in the configuration file. See [issue](https://github.com/AzureAD/microsoft-identity-web/issues/197) for more details.

**Microsoft Identity Web provides an option to force redirect URIs to use the HTTPS scheme,** which can be useful in certain scenarios, like app deployment in a container. To enable this behavior set `ForceHttpsRedirectUris` property in the configuration file. See [issue](https://github.com/AzureAD/microsoft-identity-web/issues/175) for more details.

### Bug Fixes:
**Microsoft Identity Web uses `System.Text.Json` namespace instead of `Newtonsoft.Json` for working with JSON.** See [issue](https://github.com/AzureAD/microsoft-identity-web/issues/182) for more details.

**The documentation now correctly specifies that `ClaimsPrincipalExtensions.GetNameIdentifierId` returns a `uid` claim value.** See [issue](https://github.com/AzureAD/microsoft-identity-web/issues/171) for more details.

0.1.4-preview
============
New Features:
**Microsoft Identity Web provides an option to specify if the token acquisition service should be a singleton**. See [issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/1).

Bug Fixes:
**When logging in with an unauthorized account, the user was redirected to `/Account/AccessDenied` which did not exist**. Microsoft Identity Web UI now properly sets the path on the scheme with the same name. See [issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/117).

**In the context of a guest account, Microsoft Identity Web used the `loginHint` to determine the guest account for accessing the MSAL .NET cache**. Now, Microsoft Identity Web retrieves `user_info` from the authorization server and is able to determine the unique object identifier for guest accounts. See [issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/18).

0.1.3-preview
============
New Features:
**Microsoft Identity Web now allows developers to not pass any scope value in `AddWebAppCallsProtectedWebApi`**. See [issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/14).

**When working with containers or reverse proxies, being able to specify the redirectUri and postLogoutRedirectUri is important.** Microsoft Identity Web now allows the setting of the RedirectUri and PostLogoutRedirectUri as part of the `MicrosoftIdentityOptions`. See [Issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/115).

Bug Fixes:
**The AddProtectedWebApiCallsProtectedWebApi method registers an event handler for OnTokenValidated without preserving any existing registered event handlers.** Now events are chained correctly. See [issue for details](https://github.com/AzureAD/microsoft-identity-web/issues/154).

**Depending on the endpoint, v1.0 or v2.0, and if the application is B2C or not, the default format of the `aud` value in the token will be different.** Microsoft Identity Web now looks at these parameters to validate the audience.

0.1.2-preview
============
New Features:
**Microsoft Identity Web now uses an IHttpClientFactory to implement resilient HTTP requests**. The ASP.NET Core IHttpClientFactory manages the pooling and lifetime of the underlying HttpClientMessageHandler instances, which avoids port exhaustion and common DNS problems that occur when manually managing HttpClient lifetimes. More details on this feature [here](https://github.com/AzureAD/microsoft-identity-web/issues/6).

Bug Fixes:
Performance improvement: **AadIssuerValidator class now caches the authority aliases under the correct cache key**. See [issue](https://github.com/AzureAD/microsoft-identity-web/issues/51) for more details.

**When not including the ClientSecret in appsettings.json, a null reference exception was thrown when acquiring the authorization code with MSAL.NET**. Microsoft Identity Web now checks all the required options and responds with actionable error messages if any are missing. See [issue](https://github.com/AzureAD/microsoft-identity-web/issues/66) for more details.

0.1.1-preview
============
New Features:
**Microsoft Identity Web now surfaces the ClaimConstants class**. This allows developers to build a unique ClaimsPrincipal. [See issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/100)

Bug Fixes:
**`AddSignIn()` now provides a more robust processing of authorities accepting them to end in `/` or not**. [See issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/19)

**Setting the `ValidAudiences` in `AddProtectedWebApi()` now accepts any custom audience (any string)**. [See issue for more details](https://github.com/AzureAD/microsoft-identity-web/issues/52)

0.1.0-preview
============
This is the first preview NuGet package for [Microsoft Identity Web](https://github.com/AzureAD/microsoft-identity-web/wiki).
