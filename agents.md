# Agent Automation Configuration

This document defines the rules, guidelines, and configuration for automated agents and AI assistants working with the Microsoft Identity Web repository. This replaces the previous `.clinerules` directory structure and provides comprehensive guidance for maintaining code quality, following project conventions, and implementing changes effectively.

## Table of Contents

- [Core Development Principles](#core-development-principles)
- [AI Assistant Guidelines](#ai-assistant-guidelines)
- [C# Development Standards](#c-development-standards)
- [Microsoft Identity Web Guidelines](#microsoft-identity-web-guidelines)
- [Tool Usage and Workflows](#tool-usage-and-workflows)

## Core Development Principles

### General Guidelines
- Make changes incrementally and verify each step
- Always analyze existing code patterns before making changes
- Prioritize built-in tools over shell commands
- Follow existing project patterns and conventions
- Maintain comprehensive test coverage
- Preserve file headers and license information
- Maintain consistent XML documentation
- Respect existing error handling patterns

### Quality Standards
- Follow .editorconfig rules strictly
- Verify changes match existing code style
- Ensure test coverage for new code
- Validate changes against project conventions
- Check for proper error handling
- Maintain nullable reference type annotations

## AI Assistant Guidelines

### Tool Usage Preferences

#### File Operations
- Use `read_file` for examining file contents instead of shell commands like `cat`
- Use `replace_in_file` for targeted, specific changes to existing files
- Use `write_to_file` only for new files or complete file rewrites
- Use `list_files` to explore directory structures
- Use `search_files` with precise regex patterns to find code patterns
- Use `list_code_definition_names` to understand code structure before modifications

#### Command Execution
- Use `execute_command` sparingly, preferring built-in file operation tools when possible
- Always provide clear explanations for any executed commands
- Set `requires_approval` to true for potentially impactful operations

### Development Workflow

#### Planning Phase (PLAN MODE)
- Begin complex tasks in PLAN mode to discuss approach
- Analyze existing codebase patterns using search tools
- Review related test files to understand testing patterns
- Present clear implementation steps for approval
- Ask clarifying questions early to avoid rework

#### Implementation Phase (ACT MODE)
- Make changes incrementally, one file at a time
- Verify each change before proceeding
- Follow patterns discovered during planning phase
- Focus on maintaining test coverage
- Use error messages and linter feedback to guide fixes

### MCP Server Integration
- Use appropriate MCP tools when available for specialized tasks
- Access MCP resources efficiently using proper URIs
- Handle MCP operation results appropriately
- Follow server-specific authentication and usage patterns

### Error Handling
- Provide clear error messages and suggestions
- Handle tool operation failures gracefully
- Suggest alternative approaches when primary approach fails
- Roll back changes if necessary to maintain stability

## C# Development Standards

### Language Features
- Always use the latest version C#, currently C# 13 features
- Never change global.json unless explicitly asked to
- Never change package.json or package-lock.json files unless explicitly asked to
- Never change NuGet.config files unless explicitly asked to

### Code Formatting
- Apply code-formatting style defined in `.editorconfig`
- Prefer file-scoped namespace declarations and single-line using directives
- Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.)
- Ensure that the final return statement of a method is on its own line
- Use pattern matching and switch expressions wherever possible
- Use `nameof` instead of string literals when referring to member names
- Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments

### Nullable Reference Types
- Declare variables non-nullable, and check for `null` at entry points
- Always use `is null` or `is not null` instead of `== null` or `!= null`
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null

### Testing Guidelines
- We use xUnit SDK v2 for tests
- Emit "Act", "Arrange" or "Assert" comments
- Use Moq 4.14.x for mocking in tests
- Copy existing style in nearby files for test method names and capitalization
- To build and run tests in the repo, run `dotnet test`, you need one solution open, or specify the solution

## Microsoft Identity Web Guidelines

### Overview

Microsoft Identity Web is a comprehensive authentication and authorization library for ASP.NET Core, OWIN web apps and web APIs, and daemon apps that integrate with the Microsoft identity platform and CIAM or AzureAD B2C. The library provides essential functionality for:

- Web applications that sign in users and optionally call web APIs
- Protected web APIs that may call downstream web APIs
- Daemon applications calling downstream APIs
- Token cache implementations
- Microsoft Graph integration
- Azure SDK integration

Through its modular architecture and extensive features, Microsoft Identity Web simplifies the implementation of identity and access management in modern web applications while maintaining security best practices.

### Repository Structure

#### Core Directories
- `/src` - Contains all source code for the Microsoft.Identity.Web packages
- `/tests` - Contains all test projects including unit tests, integration tests and E2E tests
- `/benchmark` - Performance benchmarking tests
- `/build` - Build scripts and configuration
- `/docs` - Documentation and blog posts
- `/ProjectTemplates` - Project templates for various ASP.NET Core scenarios
- `/tools` - Development and configuration tools

#### Project Templates
The following templates are provided:
- Blazor Server Web Applications
- Blazor WebAssembly Applications
- Azure Functions
- Razor Pages Web Applications
- ASP.NET Core MVC (Starter Web)
- ASP.NET Core Web API
- Worker Service
- Daemon app

### Shipped Packages

#### Core Packages
- Microsoft.Identity.Web - Core authentication and authorization functionality
- Microsoft.Identity.Web.UI - UI components and controllers for authentication
- Microsoft.Identity.Web.TokenCache - Token cache implementations
- Microsoft.Identity.Web.TokenAcquisition - Token acquisition functionality
- Microsoft.Identity.Web.Certificate - Certificate management and loading
- Microsoft.Identity.Web.Certificateless - Support for certificateless authentication

#### Integration Packages
- Microsoft.Identity.Web.Azure - Azure SDK integration support
- Microsoft.Identity.Web.DownstreamApi - Support for calling downstream APIs
- Microsoft.Identity.Web.OWIN - OWIN middleware integration

#### Microsoft Graph Packages
- Microsoft.Identity.Web.MicrosoftGraph - Microsoft Graph integration
- Microsoft.Identity.Web.MicrosoftGraphBeta - Microsoft Graph Beta API integration
- Microsoft.Identity.Web.GraphServiceClient - Graph SDK integration
- Microsoft.Identity.Web.GraphServiceClientBeta - Graph Beta SDK integration

#### Additional Functionality
- Microsoft.Identity.Web.Diagnostics - Diagnostic and logging support
- Microsoft.Identity.Web.OidcFIC - OpenID Connect Federated Identity Credential support

### Public and Internal API Changes

The project uses Microsoft.CodeAnalysis.PublicApiAnalyzers. For any public and internal API (i.e. public and internal member) changes:

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

## Tool Usage and Workflows

### Development Patterns
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

---

*This configuration replaces the previous `.clinerules` directory and provides unified guidance for all automated agents and AI assistants working with this repository.*