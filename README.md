# Microsoft Identity Web

[Microsoft Identity Web](https://www.nuget.org/packages/Microsoft.Identity.Web) is a library which contains a set of reusable classes used in conjunction with ASP.NET Core for integrating with the [Microsoft identity platform](https://learn.microsoft.com/azure/active-directory/develop/) (formerly *Azure AD v2.0 endpoint*) and [AAD B2C](https://learn.microsoft.com/azure/active-directory-b2c/).

This library is for specific usage with:

- [Web applications](https://github.com/AzureAD/microsoft-identity-web/wiki/web-apps), which sign in users and, optionally, call web APIs
- [Protected web APIs](https://github.com/AzureAD/microsoft-identity-web/wiki/web-apis), which optionally call protected downstream web APIs

Quick links:

| [Conceptual documentation](https://github.com/AzureAD/microsoft-identity-web/wiki) | [Getting Started](https://github.com/AzureAD/microsoft-identity-web/wiki#getting-started-with-microsoft-identity-web) | [Reference documentation](https://learn.microsoft.com/dotnet/api/microsoft.identity.web?view=azure-dotnet-preview) | [Sample Code Web App](https://github.com/AzureAD/microsoft-identity-web/wiki/web-app-samples) | [Sample Code Web API](https://github.com/AzureAD/microsoft-identity-web/wiki/web-api-samples) | [Support](README.md#community-help-and-support) |
| ------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------| ------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------- |

## Nuget package

 [![NuGet](https://img.shields.io/nuget/v/Microsoft.Identity.Web.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.Identity.Web/)

## Version Lifecycle and Support Matrix

See [Long Term Support policy](./supportPolicy.md) for details.

The following table lists IdentityWeb versions currently supported and receiving security fixes.

| Major Version | Last Release | Patch release date  | Support phase|End of support |
| --------------|--------------|--------|------------|--------|
| 3.x           | [![NuGet](https://img.shields.io/nuget/v/Microsoft.Identity.Web.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.Identity.Web/)   |Monthly| Active | Not planned.<br/>✅Supported versions: from 3.0.0 to [![NuGet](https://img.shields.io/nuget/v/Microsoft.Identity.Web.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.Identity.Web/)<br/>⚠️Unsupported versions `< 3.0.0`.|

## Build Status

[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2FAzureAD%2Fmicrosoft-identity-web%2Fbadge&style=flat)](https://actions-badge.atrox.dev/AzureAD/microsoft-identity-web/goto)

## Release notes, roadmap and SLA

### Release notes and roadmap

The Microsoft Identity Web roadmap is available from [Roadmap](https://github.com/AzureAD/microsoft-identity-web/wiki/#roadmap) in the [Wiki pages](https://github.com/AzureAD/microsoft-identity-web/wiki), along with release notes.

### Support SLA

- Major versions are supported for twelve months after the release of the next major version.
- Minor versions older than N-1 are not supported.
  > Minor versions are bugfixes or features with non-breaking (additive) API changes.  It is expected apps can upgrade.  Therefore, we will not patch old minor versions of the library. You should also confirm, in issue repros, that you are using the latest minor version before the Microsoft Identity Web team spends time investigating an issue.

## Using Microsoft Identity Web

- The conceptual documentation is currently available from the [Wiki pages](https://github.com/AzureAD/microsoft-identity-web/wiki).
- Code samples are available for [web app samples](https://github.com/AzureAD/microsoft-identity-web/wiki/web-app-samples)
  and [web API samples](https://github.com/AzureAD/microsoft-identity-web/wiki#web-api-samples)

### Authority Configuration

Understanding how to properly configure authentication authorities is crucial for Azure AD, B2C, and CIAM applications:

- **[Authority Configuration & Precedence Guide](docs/authority-configuration.md)** - Comprehensive guide explaining how Authority, Instance, TenantId, and PreserveAuthority work together
- **[B2C Authority Examples](docs/b2c-authority-examples.md)** - Azure AD B2C-specific configuration patterns and best practices
- **[CIAM Authority Examples](docs/ciam-authority-examples.md)** - Customer Identity Access Management (CIAM) scenarios with custom domains
- **[Migration Guide](docs/migration-authority-vs-instance.md)** - Upgrade path for existing applications and resolving configuration conflicts
- **[Authority FAQ](docs/faq-authority-precedence.md)** - Common questions and troubleshooting tips

## Where do I file issues


This is the correct repo to file [issues](https://github.com/AzureAD/microsoft-identity-web/issues).

## Community Help and Support

If you find a bug or have a feature or documentation request, please raise the issue on [GitHub Issues](https://github.com/AzureAD/microsoft-identity-web/issues).

We use [Stack Overflow](http://stackoverflow.com/questions/) with the community to provide support, using the tags `web-app`, `web-api`, `asp.net-core`, `microsoft-identity-web`. We highly recommend you ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before.

To provide a recommendation, visit our [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contribute

We enthusiastically welcome contributions and feedback. You can clone the repo and start contributing now. Read our [Contribution Guide](CONTRIBUTING.md) for more information.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Security Library

This library controls how users sign-in and access services. We recommend you always take the latest version of our library in your app when possible. We use [semantic versioning](http://semver.org) so you can control the risk associated with updating your app. As an example, always downloading the latest minor version number (e.g. x.*y*.x) ensures you get the latest security and feature enhancements, but our API surface remains the same. You can always see the latest version and release notes under the Releases tab of GitHub.

## Security Reporting

If you find a security issue with our libraries or services, please report it to [secure@microsoft.com](mailto:secure@microsoft.com) with as much detail as possible. Your submission may be eligible for a bounty through the [Microsoft Bounty](http://aka.ms/bugbounty) program. Please do not post security issues to GitHub Issues or any other public site. We will contact you shortly upon receiving the information. We encourage you to get notifications of when security incidents occur by visiting [this page](https://technet.microsoft.com/en-us/security/dd252948) and subscribing to Security Advisory Alerts.

## Building with .NET Preview Versions

Microsoft Identity Web supports building and testing with .NET preview versions using the `TargetNetNext` conditional compilation flag. This enables early testing and compatibility validation with the latest .NET preview releases.

### Prerequisites

- Install .NET 10 preview SDK from [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0)

### Building with .NET 10 Preview

To build the solution with .NET 10 preview support:

```bash
# Build with .NET 10 preview targets included
dotnet build Microsoft.Identity.Web.sln -p:TargetNetNext=True

# Or using MSBuild
msbuild Microsoft.Identity.Web.sln -p:TargetNetNext=True
```
You can also set the TargetNetNext environment variable on your machine with the value `True`.

### Testing with .NET 10 Preview

To run tests targeting .NET 10 preview:

```bash
# Run tests with .NET 10 preview (conditional)
dotnet test Microsoft.Identity.Web.sln -f net10.0 -p:TargetNetNext=True
```

**Note:** .NET 10 preview support is conditional and requires setting `TargetNetNext=True` during build/test operations. This ensures compatibility with the latest preview versions while maintaining stability for production builds.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

Copyright (c) Microsoft Corporation.  All rights reserved. Licensed under the MIT License (the "License").
