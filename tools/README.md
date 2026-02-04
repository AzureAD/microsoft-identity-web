# Microsoft Identity Web - Tools Directory

This directory contains various tools and utilities used for development, testing, and maintenance of the Microsoft Identity Web library. Each tool serves a specific purpose in the development workflow.

## Table of Contents

- [PowerShell Scripts](#powershell-scripts)
  - [Check-BrokenLinks.ps1](#check-brokenlinksps1)
  - [mark-shipped.ps1](#mark-shippedps1)
- [.NET Tools](#net-tools)
  - [ConfigureGeneratedApplications](#configuregeneratedapplications)
  - [GenerateMergeOptionsMethods](#generatemergeoptionsmethods)
  - [CrossPlatformValidator](#crossplatformvalidator)
  - [app-provisioning-tool](#app-provisioning-tool)

---

## PowerShell Scripts

### Check-BrokenLinks.ps1

**Purpose:** Validates markdown documentation integrity by detecting broken internal links across the repository.

**Functionality:**
- Recursively scans all `.md` files in the repository
- Extracts markdown links in the format `[text](target)`
- Verifies that target files exist (skips external URLs, anchors, and mailto links)
- Optionally checks external HTTP/HTTPS links for validity

**Usage:**
```powershell
.\Check-BrokenLinks.ps1 [-Path <path>] [-IncludeExternal] [-OutputFormat {Table|List|Json|Csv}]
```

**Parameters:**
- `-Path`: Root path to start scanning (defaults to current directory)
- `-IncludeExternal`: Include validation of external HTTP/HTTPS links
- `-OutputFormat`: Format of the output report (Table, List, Json, or Csv)

**Output:** Reports broken links grouped by pattern (scenarios/, deployment/, etc.)

---

### mark-shipped.ps1

**Purpose:** Manages the .NET API surface by moving unshipped APIs to shipped APIs and cleaning up the unshipped API files.

**Functionality:**
- Processes both Public API and Internal API tracking files
- Reads unshipped APIs from `PublicAPI.Unshipped.txt` and `InternalAPI.Unshipped.txt`
- Moves non-removed API items to the corresponding `Shipped.txt` files
- Filters out items marked with the `*REMOVED*` prefix
- Clears unshipped files and reinitializes them with `#nullable enable`

**Key Files:**
- `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`
- `InternalAPI.Shipped.txt` / `InternalAPI.Unshipped.txt`

**Usage:**
```powershell
.\mark-shipped.ps1
```

Runs without arguments and processes all API files recursively throughout the repository.

---

## .NET Tools

### ConfigureGeneratedApplications

**Purpose:** A C# .NET 8.0 console application that configures project template applications by replacing placeholders with parameter values from JSON configuration files.

**Key Files:**
- `Program.cs` - Main application logic (JSON parsing, file processing, replacements)
- `Configuration.json` - Configuration file containing parameters and project definitions
- Model classes: `Configuration`, `Project`, `File`, `PropertyMapping`, `Replacement`

**Workflow:**
1. Reads `Configuration.json` containing project template definitions
2. For each project, processes specified JSON files
3. Locates properties using JSON path notation (e.g., `database:connectionString`)
4. Replaces placeholder values with actual parameter values
5. Generates an `issue.md` file with a testing checklist for the configured applications

**Technology:** .NET 8.0 Console Application

**Output:** Updated project files with configured values and a testing checklist

---

### GenerateMergeOptionsMethods

**Purpose:** A code generator tool that creates merge/update methods for synchronizing properties between different option classes.

**Functionality:**
- Uses reflection to inspect two types and their properties
- Generates C# code for property synchronization
- Handles different property types appropriately:
  - String properties: Uses `string.IsNullOrWhiteSpace()` checks
  - Value types: Direct assignment
  - Reference types: Null checks before assignment
- Currently generates `UpdateMicrosoftIdentityApplicationOptionsFromMergedOptions()` method

**Technology:** .NET 8.0 Console Application

**Output:** Generated C# code for updating one option type from another (printed to console, intended to be copied into source files)

**Use Case:** Reduces manual coding errors when creating merge methods between option classes in the library.

---

### CrossPlatformValidator

**Purpose:** A cross-platform JWT token validation library suite designed for testing authentication across different .NET frameworks and platforms.

**Structure:**

#### Core Library (CrossPlatformValidation)
The main validation library that provides JWT token validation capabilities.

**Key Components:**
- `RequestValidator` - Validates JWT tokens against Azure AD/Microsoft Entra ID
- `EntryPoint` - Exposes validation functionality via P/Invoke for use from unmanaged code (C/C++)
- Supports both `JwtSecurityTokenHandler` and `JsonWebTokenHandler`

**Functionality:**
- Initializes with authority URL and audience
- Validates bearer tokens using Azure AD OIDC configuration
- Returns `TokenValidationResult` containing claims and issuer information

#### Supporting Projects
- **CSharpConsoleApp** - Example console application demonstrating validator usage
- **BenchmarkCSharp** - Performance benchmarking tool for measuring validation performance
- **CrossPlatformValidatorTests** - Comprehensive test suite for the validator

**Technology:** Multi-targeting .NET library with C# examples and tests

**Use Case:** Testing authentication scenarios across different platforms and .NET framework versions.

---

### app-provisioning-tool

**Purpose:** A dotnet CLI tool (`msidentity-app-sync`) that creates and updates Azure AD and Azure AD B2C app registrations, and automatically configures ASP.NET Core application code.

**Package Name:** `msidentity-app-sync`

**Structure:**
- `app-provisioning-tool/` - CLI executable project
- `app-provisioning-lib/` - Core provisioning library
- `tests/` - Test suite
- `images/`, `README.md`, `vs2019-16.9-how-to-use.md` - Documentation and resources

**Capabilities:**
- Auto-detects application type (webapp, mvc, webapi, blazor)
- Creates Azure AD and Azure AD B2C app registrations
- Updates configuration files (`appsettings.json`, `Program.cs`, etc.)
- Handles redirect URIs from launch settings
- Supports configuring existing applications via `--client-id` parameter
- Manages app secrets and authentication configuration

**Installation:**

Global installation from NuGet:
```bash
dotnet tool install --global msidentity-app-sync
```

Build and install from repository:
```bash
cd tools/app-provisioning-tool
dotnet build
dotnet pack
dotnet tool install --global --add-source ./app-provisioning-tool/bin/Debug msidentity-app-sync
```

**Usage:**

Run in your ASP.NET Core project folder:
```bash
msidentity-app-sync
```

The tool will:
- Detect your application configuration automatically
- Prompt for tenant and identity provider information if needed
- Create or update Azure AD app registration
- Update your application's configuration files

**Parameters:**
- `--client-id <id>` - Use an existing app registration
- `--tenant-id <id>` - Specify the tenant
- Other parameters for customizing the provisioning process

**Documentation:** See `app-provisioning-tool/README.md` for detailed usage instructions and `vs2019-16.9-how-to-use.md` for Visual Studio integration.

**Use Case:** Streamlines the process of setting up Azure AD authentication in ASP.NET Core applications, particularly useful for developers and during project setup.

---

## Summary

| Tool | Type | Primary Use Case |
|------|------|------------------|
| **Check-BrokenLinks.ps1** | PowerShell Script | Documentation quality assurance - validates markdown links |
| **mark-shipped.ps1** | PowerShell Script | API management - tracks shipped vs unshipped API changes |
| **ConfigureGeneratedApplications** | .NET Console App | Project template configuration - sets up test applications |
| **GenerateMergeOptionsMethods** | .NET Console App | Code generation - creates property merge methods |
| **CrossPlatformValidator** | .NET Library | Authentication testing - validates JWT tokens cross-platform |
| **app-provisioning-tool** | .NET CLI Tool | Azure AD provisioning - automates app registration and config |

---

## Contributing

When adding new tools to this directory:
1. Ensure the tool has a clear, single purpose
2. Include documentation (README or inline comments)
3. Add an entry to this README describing the tool's purpose and usage
4. Consider whether the tool should be a standalone utility or integrated into the build process

---

## Additional Resources

- [Microsoft Identity Web Documentation](../README.md)
- [Contributing Guidelines](../CONTRIBUTING.md)
- [Testing Guide](../TESTING.md)
