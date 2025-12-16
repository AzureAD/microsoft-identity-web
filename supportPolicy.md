# Microsoft. Identity.Web Support Policy

_Last updated December 15, 2025_

## Supported versions
The following table lists IdentityWeb versions currently supported and receiving security fixes.

| Major Version | Last Release | Patch Release Date  | Support Phase| End of Support |
| --------------|--------------|--------|------------|--------|
| 4.x           | [![NuGet](https://img.shields.io/nuget/v/Microsoft.Identity. Web.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.Identity.Web/) | Monthly | Active | TBD |
| 3.x           | 3.14.1       | August 28, 2025 | Maintenance (Security fixes only) | August 2026 |

## Out of support versions
The following table lists Microsoft. Identity.Web versions no longer supported and no longer receiving security fixes.

| Major Version | Latest Patch Version| Patch Release Date | End of Support Date|
| --------------|--------------|--------|--------|
| 2.x           | 2.21.0       | July 18, 2024      | January 1, 2025 |
| 1.x           | 1.26.0       | February 5, 2023   | February 5, 2023|

## Migration Guidance

### ⚠️ For Users on Version 2.x or Earlier
**These versions are no longer supported.** Please upgrade to version 4.x immediately to receive security updates and bug fixes.

### For Users on Version 3.x
While version 3.x currently receives security fixes, it is in maintenance mode. We recommend planning your migration to version 4.x. Support for 3.x will end in August 2026.

### Migration Resources
- [Migration Guide to v4](https://github.com/AzureAD/microsoft-identity-web/blob/master/MIGRATION_GUIDE_V4.md)
- [What's New in 4.0](https://github.com/AzureAD/microsoft-identity-web/releases/tag/4.0.0)
- [View all releases](https://github.com/AzureAD/microsoft-identity-web/releases)

## Overview

Every Microsoft product has a lifecycle.  The lifecycle begins when a product is released and ends when it's no longer supported.  Knowing key dates in this lifecycle helps you make informed decisions about when to upgrade or make other changes to your software.

The Microsoft suite of auth libraries provides comprehensive tools for identity and security token processing in . NET, and non-. NET, applications, including authentication, authorization, token validation, and more.

## Understanding Support Phases

- **Active Support**: Latest major version receives new features, bug fixes, and security updates with monthly patch releases
- **Maintenance Support**:  Previous major version receives security fixes only for approximately 9-12 months after new major release
- **End of Support**: No further updates of any kind

## Support Policy Guiding Principles
The support policy can be summarized by three key rules:
1. **"Last Major Release" Support Window:** For each major version of the library (v5, v6, v7, v8, etc.), only the latest patch release of that major version is officially supported once a new major version is released. 
2. **Deprecation of Older Versions on New Major Release:** When a new major version of the library is released (e.g., 4.0.0), all previous minor/patch versions of the previous major (e.g., 3.x) enter maintenance mode with security fixes only. 
3. **Security Fixes Only in Supported Versions:** Security fixes and critical bug fixes will be provided only for the supported versions – namely, the latest patch of the latest major, and in some cases, the latest patch of the previous major version during its maintenance window. 
