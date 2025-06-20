# Microsoft Identity Web Guidelines

## Overview

Microsoft Identity Web is a comprehensive authentication and authorization library for ASP.NET Core, OWIN web apps and web APIs, and daemon apps that integrate with the Microsoft identity platform and CIAM or AzureAD B2C. The library provides essential functionality for:

- Web applications that sign in users and optionally call web APIs
- Protected web APIs that may call downstream web APIs
- Daemon applications calling downstream APIs
- Token cache implementations
- Microsoft Graph integration
- Azure SDK integration

Through its modular architecture and extensive features, Microsoft Identity Web simplifies the implementation of identity and access management in modern web applications while maintaining security best practices.

## Repository Structure

### Core Directories
- `/src` - Contains all source code for the Microsoft.Identity.Web packages
- `/tests` - Contains all test projects including unit tests, integration tests and E2E tests
- `/benchmark` - Performance benchmarking tests
- `/build` - Build scripts and configuration
- `/docs` - Documentation and blog posts
- `/ProjectTemplates` - Project templates for various ASP.NET Core scenarios
- `/tools` - Development and configuration tools

### Project Templates
The following templates are provided:
- Blazor Server Web Applications
- Blazor WebAssembly Applications
- Azure Functions
- Razor Pages Web Applications
- ASP.NET Core MVC (Starter Web)
- ASP.NET Core Web API
- Worker Service
- Daemon app

## Shipped DLLs/Packages

The following NuGet packages are shipped as part of Microsoft.Identity.Web:

### Core Packages
- Microsoft.Identity.Web - Core authentication and authorization functionality
- Microsoft.Identity.Web.UI - UI components and controllers for authentication
- Microsoft.Identity.Web.TokenCache - Token cache implementations
- Microsoft.Identity.Web.TokenAcquisition - Token acquisition functionality
- Microsoft.Identity.Web.Certificate - Certificate management and loading
- Microsoft.Identity.Web.Certificateless - Support for certificateless authentication

### Integration Packages
- Microsoft.Identity.Web.Azure - Azure SDK integration support
- Microsoft.Identity.Web.DownstreamApi - Support for calling downstream APIs
- Microsoft.Identity.Web.OWIN - OWIN middleware integration

### Microsoft Graph Packages
- Microsoft.Identity.Web.MicrosoftGraph - Microsoft Graph integration
- Microsoft.Identity.Web.MicrosoftGraphBeta - Microsoft Graph Beta API integration
- Microsoft.Identity.Web.GraphServiceClient - Graph SDK integration
- Microsoft.Identity.Web.GraphServiceClientBeta - Graph Beta SDK integration

### Additional Functionality
- Microsoft.Identity.Web.Diagnostics - Diagnostic and logging support
- Microsoft.Identity.Web.OidcFIC - OpenID Connect Federated Identity Credential support

## Development Guidelines

### Common Development Patterns
- Follow .editorconfig rules strictly
- Ensure proper error handling
- Maintain test coverage for changes
- Document API changes thoroughly
- Keep configuration consistent with project standards

### Testing Requirements
- Include tests for all code changes
- Follow existing test patterns
- Include benchmark tests for performance-sensitive changes
- Verify security implications of changes

### Public and Internal API Changes
- The project uses Microsoft.CodeAnalysis.PublicApiAnalyzers
- For any public and internal API (i.e. public and internal member) changes:
  1. Update PublicAPI.Unshipped.txt in the relevant package directory for a public API change
  2. Update InternalAPI.Unshipped.txt in the relevant package directory for an internal API change
  3. Include complete API signatures
  4. Consider backward compatibility impacts
  5. Document breaking changes clearly

Example format:
```diff
// Adding new API
+MyNamespace.MyClass.MyNewMethod() -> void
+MyNamespace.MyClass.MyProperty.get -> string
+MyNamespace.MyClass.MyProperty.set -> void

// Removing API
-MyNamespace.MyClass.OldMethod() -> void
```

The analyzer enforces documentation of all public API changes in PublicAPI.Unshipped.txt and all internal API changes in InternalAPI.Unshipped.txt and will fail the build if changes are not properly reflected.
