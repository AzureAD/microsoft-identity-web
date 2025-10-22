# Microsoft.Identity.Web Documentation Modernization Plan

**Status:** In Progress  
**Branch:** `feature/doc-modernization`  
**Started:** October 6, 2025  
**Contributors:** Jean-Marc Prieur (@jmprieur), GitHub Copilot  

---

## 🎯 Project Goals

Modernize the Microsoft.Identity.Web documentation by:

1. **Migrating from GitHub Wiki to `/docs` folder** - Make documentation version-controlled and easier to maintain
2. **Updating for current versions** - Focus on v3.14.1 (stable) and upcoming v4.0.0
3. **Modernizing for .NET 8 & .NET 9** - Update all code examples and guidance
4. **Improving discoverability** - Clear navigation, scenario-based organization
5. **Highlighting new features** - CIAM, certificateless auth, agent identities, etc.
6. **Providing migration guidance** - Help users transition from old wiki to new docs

---

## ⏰ Key Milestones

| Milestone | Target | Status |
|-----------|--------|--------|
| Foundation (README + Quickstarts) | Week of Oct 6 | ✅ Complete |
| Credentials Documentation | Week of Oct 6 | 🚀 In Progress |
| Calling Downstream APIs | Week of Oct 6 | 📝 Next |
| Agent Identities Scenario | Before Ignite Nov 2025 | 📝 Upcoming |
| Core Scenarios (Web Apps, APIs, Daemon) | TBD | 📝 TODO |

---

## 📐 Documentation Structure (Approved)

```
docs/
├── README.md                          ✅ COMPLETE - Main home page
├── getting-started/
│   ├── quickstart-web-app.md          ✅ COMPLETE - Updated with explicit auth schemes
│   ├── quickstart-web-api.md          ✅ COMPLETE - Updated with explicit auth schemes
│   └── why-microsoft-identity-web.md  📝 TODO
├── calling-downstream-apis/           🚀 HIGH PRIORITY - Transverse scenario
│   ├── README.md                      (Overview, patterns, IDownstreamApi)
│   ├── from-web-apps.md               (Web app specific aspects)
│   ├── from-web-apis.md               (Web API specific aspects - OBO)
│   ├── microsoft-graph.md             (GraphServiceClient integration)
│   ├── azure-sdks.md                  (MicrosoftIdentityTokenCredential)
│   └── custom-apis.md                 (IAuthorizationHeaderProvider)
├── scenarios/
│   ├── web-apps/                      📝 TODO - ASP.NET Core + OWIN
│   │   ├── README.md
│   │   ├── sign-in-users.md
│   │   ├── incremental-consent.md     (⭐ Important)
│   │   ├── hybrid-spa.md
│   │   └── troubleshooting.md
│   ├── web-apis/                      📝 TODO - ASP.NET Core + OWIN
│   │   ├── README.md
│   │   ├── protect-api.md
│   │   ├── long-running-processes.md  (⭐ Important - OBO scenarios)
│   │   ├── behind-gateways.md         (⭐ Important)
│   │   ├── grpc.md
│   │   ├── authorization-policies.md
│   │   └── troubleshooting.md
│   ├── daemon/                        📝 TODO
│   │   ├── README.md
│   │   ├── client-credentials.md
│   │   ├── managed-identity.md
│   │   └── worker-services.md
│   ├── agent-identities/              🎯 IGNITE 2025 - Critical for November
│   │   ├── README.md                  (Code-focused, complements platform docs)
│   │   ├── setup-configuration.md     (App registration, credentials)
│   │   ├── calling-apis.md            (Calling APIs on behalf of agents)
│   │   ├── sidecar-container.md       (NEW - sidecar branch integration)
│   │   └── samples.md                 (Working code examples)
│   └── azure-functions/               📝 TODO
│       └── README.md
├── authentication/
│   ├── credentials/                   🚀 NEXT - Top priority (prerequisite)
│   │   ├── README.md                  (Comprehensive decision guide)
│   │   ├── certificateless.md         (FIC+MSI deep dive)
│   │   ├── certificates.md            (All cert types)
│   │   ├── client-secrets.md          (Dev/test only)
│   │   └── token-decryption.md        (Special case)
│   └── token-cache/                   📝 TODO
│       ├── README.md
│       ├── serialization.md
│       ├── distributed-cache.md
│       └── troubleshooting.md
├── packages/                          📝 TODO - Document new packages
│   ├── README.md                      (Package overview)
│   ├── microsoft-identity-web.md      (Core package)
│   ├── token-acquisition.md
│   ├── downstream-api.md
│   ├── azure.md                       (NEW - MicrosoftIdentityTokenCredential)
│   ├── agent-identities.md            (NEW - Agent support)
│   ├── certificateless.md             (NEW - Certificateless auth)
│   ├── diagnostics.md                 (NEW - Diagnostics)
│   └── owin.md                        (.NET Framework support)
├── advanced/                          📝 TODO
│   ├── multiple-auth-schemes.md       (Include explicit scheme discussion)
│   ├── incremental-consent-ca.md      (⭐ Important)
│   ├── long-running-processes.md      (⭐ Important - detailed OBO patterns)
│   ├── api-gateways.md                (⭐ Important)
│   ├── customization.md
│   ├── performance.md
│   └── logging.md
├── deployment/                        📝 TODO
│   ├── azure-app-service.md
│   ├── containers.md
│   └── proxies.md
├── migration/                         📝 TODO - Lower priority
│   ├── v1-to-v2.md
│   ├── v2-to-v3.md
│   └── v3-to-v4.md                    (NEW - for upcoming 4.0.0)
└── wiki-archive/                      ✅ COMPLETE - Preserved for reference
    └── (All original wiki .md files)
```

---

## ✅ Completed Work

### Phase 1: Foundation (October 6, 2025)

**1. Main Documentation Home (docs/README.md)** ✅
- Updated intro to cover ASP.NET Core, OWIN, and .NET
- Reorganized "What's Included" with token acquisition under downstream APIs
- Added "Calling APIs with Automatic Authentication" section
- Included agent identities reference
- Updated all code examples to use explicit authentication schemes
- Corrected daemon scenario code to use `TokenAcquirerFactory`
- Added configuration approaches section (appsettings.json + code)
- Added appsettings.json copy behavior note
- Included ClientCredentials note with credentials guide link
- Mentioned `IAuthorizationHeaderProvider` as alternative to `IDownstreamApi`
- Documented .NET version support (focus on .NET 8 & .NET 9)
- Version: 3.14.1 stable, preparing for 4.0.0

**2. Web App Quickstart (docs/getting-started/quickstart-web-app.md)** ✅
- Updated to use explicit authentication scheme pattern
- `.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp()`
- .NET 9 code examples
- Complete app registration setup guide

**3. Web API Quickstart (docs/getting-started/quickstart-web-api.md)** ✅
- Updated to use explicit authentication scheme pattern
- `.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi()`
- .NET 9 code examples
- Complete app registration and testing guide

**4. Wiki Archive** ✅
- Preserved all original wiki content in `docs/wiki-archive/`
- Available for reference and migration mapping

**5. Documentation Modernization Plan** ✅
- Comprehensive roadmap created
- Priorities and timeline established
- Living document for tracking progress

---

## 🚀 Current Sprint: Week of October 6, 2025

### Priority 1: Credentials Documentation (Hybrid Approach) - IN PROGRESS

**Goal:** Create comprehensive credential documentation as prerequisite for agent identities

#### Files to Create:

1. **docs/authentication/credentials/README.md** (Main hub - 20-30 min read)
   - Overview of authentication approaches
   - Decision flow chart (Mermaid diagram)
   - Comparison table (when to use what)
   - Quick start examples for all credential types
   - Links to detailed guides
   - Security best practices

2. **docs/authentication/credentials/certificateless.md** (15-20 min read)
   - FIC + Managed Identity deep dive
   - Benefits and use cases
   - Azure deployment setup
   - Configuration examples (JSON + code)
   - System-assigned vs user-assigned MSI
   - Troubleshooting
   - Migration from certificates

3. **docs/authentication/credentials/certificates.md** (25-30 min read)
   - All certificate types (Key Vault, Store, File, Base64)
   - Key Vault setup and integration
   - Certificate rotation strategies
   - Store management (Windows)
   - Development with file-based certificates
   - Configuration examples (JSON + code)
   - Troubleshooting

4. **docs/authentication/credentials/client-secrets.md** (10 min read)
   - When to use (development/testing only)
   - How to configure
   - Key Vault integration for secrets
   - Security warnings and best practices
   - Migration to production-ready credentials

5. **docs/authentication/credentials/token-decryption.md** (10-15 min read)
   - AutoDecryptKeys overview
   - Use cases for token decryption
   - Configuration examples
   - Integration with credentials

**Content Source:**
- Adapt credentialdescription.md from microsoft-identity-abstractions
- Add Microsoft.Identity.Web-specific patterns
- Include ASP.NET Core and OWIN examples

---

### Priority 2: Calling Downstream APIs - NEXT (Prerequisite for Agent Identities)

**Goal:** Create transverse documentation covering API calling patterns across all scenarios

#### Files to Create:

1. **docs/calling-downstream-apis/README.md** (Main overview)
   - What is a downstream API call?
   - Token acquisition patterns
   - `IDownstreamApi` overview
   - `IAuthorizationHeaderProvider` overview
   - When to use each approach
   - Error handling and retries
   - Cross-references to scenarios

2. **docs/calling-downstream-apis/from-web-apps.md**
   - Acquiring tokens on behalf of signed-in user
   - Incremental consent
   - Handling token acquisition failures
   - ASP.NET Core examples
   - OWIN examples (if different)

3. **docs/calling-downstream-apis/from-web-apis.md**
   - On-Behalf-Of (OBO) flow
   - Long-running processes with OBO
   - Token caching considerations
   - Error handling specific to APIs
   - ASP.NET Core examples
   - OWIN examples (if different)

4. **docs/calling-downstream-apis/microsoft-graph.md**
   - `GraphServiceClient` integration
   - Using `AddMicrosoftGraph()` extensions
   - Delegated permissions vs app permissions
   - Common Graph API patterns
   - Batching and paging

5. **docs/calling-downstream-apis/azure-sdks.md** ⭐ NEW
   - **MicrosoftIdentityTokenCredential from Microsoft.Identity.Web.Azure**
   - Integrating with Azure SDK clients (Storage, KeyVault, etc.)
   - Configuration patterns
   - Code examples for common Azure services
   - Managed Identity integration

6. **docs/calling-downstream-apis/custom-apis.md**
   - Calling your own protected APIs
   - Configuring BaseUrl, scopes, relative paths
   - Using `IAuthorizationHeaderProvider` for custom HTTP logic
   - Adding custom headers
   - Handling API-specific authentication patterns

**Key Content Elements:**
- Token acquisition flows (delegated, app-only, OBO)
- Configuration patterns (JSON + code)
- ASP.NET Core and OWIN where applicable
- Error handling and troubleshooting
- Links to related credential and scenario documentation

---

### Priority 3: Agent Identities Scenario (Before Ignite November 2025) 🎯

**Goal:** Provide complete code-focused documentation for agent identities feature for Ignite 2025

**Context:**
- Microsoft Identity Platform will provide conceptual/architectural documentation
- Microsoft.Identity.Web docs focus on implementation and code
- New sidecar container from `sidecar` branch needs integration

#### Files to Create:

1. **docs/scenarios/agent-identities/README.md**
   - What are agent identities? (brief, link to platform docs)
   - Why use agent identities?
   - Prerequisites and setup
   - Quick start example
   - Navigation to detailed topics

2. **docs/scenarios/agent-identities/setup-configuration.md**
   - App registration for agent identities
   - Configuring credentials (reference credentials docs)
   - Required permissions and scopes
   - Configuration examples (JSON + code)
   - Common setup issues

3. **docs/scenarios/agent-identities/calling-apis.md**
   - Calling APIs on behalf of agent identities
   - Token acquisition for agents
   - Using `IDownstreamApi` with agents
   - Using `IAuthorizationHeaderProvider` with agents
   - Code examples and patterns
   - Error handling specific to agents

4. **docs/scenarios/agent-identities/sidecar-container.md** ⭐ NEW
   - What is the sidecar container?
   - When to use the sidecar approach
   - Integration with `sidecar` branch
   - Deployment and configuration
   - Docker/container setup
   - Troubleshooting sidecar issues

5. **docs/scenarios/agent-identities/samples.md**
   - Complete working examples
   - Sample scenarios (common use cases)
   - Links to sample repositories
   - Step-by-step walkthroughs

**Timeline:**
- Target completion: Before Ignite 2025 (November)
- Prerequisites complete this week (credentials + downstream APIs)
- Coordinate with sidecar branch work

---

## 📝 Future Priorities (After Current Sprint)

### Phase 3: Core Scenario Deep Dives

**Web Apps Scenario** (ASP.NET Core + OWIN)
- Complete scenario documentation
- Sign-in patterns
- Incremental consent
- Hybrid SPA support
- Use contextual tabs/sections for ASP.NET Core vs OWIN differences

**Web APIs Scenario** (ASP.NET Core + OWIN)
- Complete scenario documentation
- Protecting APIs
- Long-running processes (OBO deep dive)
- APIs behind gateways
- Use contextual tabs/sections for ASP.NET Core vs OWIN differences

**Daemon Scenario**
- Complete guide based on TokenAcquirerFactory
- Client credentials flow
- Managed identity usage
- Worker services pattern

### Phase 4: Package Documentation

**New packages requiring documentation:**
1. Microsoft.Identity.Web.Azure (MicrosoftIdentityTokenCredential) ⭐
2. Microsoft.Identity.Web.AgentIdentities ⭐
3. Microsoft.Identity.Web.Certificateless
4. Microsoft.Identity.Web.Diagnostics
5. Microsoft.Identity.Web.OidcFIC

**Existing packages to update:**
- Microsoft.Identity.Web (core)
- Microsoft.Identity.Web.DownstreamApi
- Microsoft.Identity.Web.TokenAcquisition
- Microsoft.Identity.Web.GraphServiceClient
- Microsoft.Identity.Web.OWIN

### Phase 5: Advanced Topics

1. Multiple Authentication Schemes (include explicit scheme explanation)
2. Incremental Consent & Conditional Access
3. Long-Running Processes (detailed OBO patterns)
4. APIs Behind Gateways (Azure API Management, Front Door)
5. Performance Optimization
6. Logging & Diagnostics
7. Customization

### Phase 6: Migration & Mapping

1. **Wiki → New Docs Mapping Document**
   - Table showing old wiki page → new docs location
   - Add links in wiki archive pointing to new docs
   - Deprecation notices

2. **Version Migration Guides**
   - v3 to v4 migration guide (for upcoming 4.0.0 release)
   - Update existing v1→v2 and v2→v3 guides if needed

---

## 🎯 Key Decisions & Patterns Established

### Code Patterns (Approved)

**Web Apps (ASP.NET Core):**
```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
```

**Web APIs (ASP.NET Core):**
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

**Daemon Apps:**
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

var tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
tokenAcquirerFactory.Services.AddDownstreamApi("MyApi",
    tokenAcquirerFactory.Configuration.GetSection("MyWebApi"));
var sp = tokenAcquirerFactory.Build();
var api = sp.GetRequiredService<IDownstreamApi>();
```

**OWIN (Web Apps and Web APIs):**
- TBD based on scenario documentation needs
- Will show contextual differences from ASP.NET Core

### Documentation Principles (Approved)

1. **Explicit over abstracted** - Show explicit authentication schemes for ASP.NET Core
2. **Scenario-based organization** - Start with scenarios, then dive into details
3. **Progressive disclosure** - Quickstarts → Scenarios → Advanced topics
4. **Hybrid approach for complex topics** - Comprehensive README + detailed guides
5. **Transverse scenarios** - Shared patterns (like downstream APIs) get dedicated sections
6. **Configuration flexibility** - Show both appsettings.json and code approaches
7. **Platform support clarity** - Use contextual tabs/sections for ASP.NET Core vs OWIN
8. **.NET version focus** - .NET 8 (LTS) and .NET 9 (latest) in examples
9. **Version awareness** - Document current stable (3.14.1), note upcoming (4.0.0)

### Content Priorities (Established)

**This Week (Critical Path for Agent Identities):**
1. 🚀 Credentials documentation (prerequisite)
2. 🚀 Calling Downstream APIs documentation (prerequisite)

**Before Ignite November 2025:**
3. 🎯 Agent Identities scenario (including sidecar container)

**High Priority (After Ignite Prep):**
4. 📝 Core scenario deep dives (Web Apps, Web APIs, Daemon)
5. 📝 Package documentation (Azure, AgentIdentities, etc.)

**Medium Priority:**
6. 📝 Advanced topics (auth schemes, long-running, gateways, incremental consent)

**Lower Priority:**
7. 📝 Migration guides (v3→v4 when 4.0.0 releases)
8. 📝 Deployment guides

**Deprioritized:**
- B2C-specific content (focus on CIAM instead)

---

## 🔧 Configuration Standards

### appsettings.json Pattern

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientCredentials": [
      {
        "SourceType": "SignedAssertionFromManagedIdentity"
      }
    ]
  }
}
```

**Important Notes:**
- For daemon/console apps: Set "Copy to Output Directory" = "Copy if newer"
- ClientCredentials array supports multiple types (see credentials guide)
- Both JSON and code configuration are valid for all scenarios

---

## 🎨 ASP.NET Core vs OWIN Presentation

### Approach for Contextual Documentation

**Decision:** Use contextual sections/tabs based on what GitHub Markdown supports

**Research needed:** Determine best GitHub Markdown approach:
- Option 1: HTML `<details>` tags for expandable sections
- Option 2: Clear section headers with platform labels
- Option 3: Side-by-side code blocks with clear labels
- Option 4: GitHub-flavored Markdown tabs (if supported)

**Example pattern (to be refined):**

```markdown
## Configure Authentication

### For ASP.NET Core

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
```

### For OWIN (.NET Framework)

```csharp
app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
{
    ClientId = Configuration["AzureAd:ClientId"],
    Authority = $"{Configuration["AzureAd:Instance"]}{Configuration["AzureAd:TenantId"]}/v2.0",
    // Additional OWIN configuration
});
```

**Will be determined:** Best visual approach based on GitHub Markdown capabilities

---

## 🎨 Visual Elements to Include

### Mermaid Diagrams Planned

1. **Credentials Decision Flow** (in credentials/README.md)
   - Flowchart helping developers choose authentication method
   - Based on: Azure? Production? Rotation needed? etc.

2. **Downstream API Call Flow** (in calling-downstream-apis/README.md)
   - Token acquisition and caching flow
   - Different patterns (delegated, OBO, app-only)

3. **Agent Identities Architecture** (in scenarios/agent-identities/README.md)
   - How agent identities work
   - Sidecar container integration
   - Token flow for agents

4. **Documentation Navigation** (optional - in main README.md)
   - Visual map of documentation structure

---

## 📊 Success Metrics

How we'll know this modernization is successful:

1. **Completeness** - All high-priority sections documented before Ignite
2. **Accuracy** - Code examples work with .NET 8/9 and v3.14.1+
3. **Discoverability** - Users can find what they need quickly
4. **Maintainability** - Version-controlled, easy to update
5. **Migration clarity** - Clear path from old wiki to new docs
6. **Ignite readiness** - Agent identities documentation ready for public presentation

---

## 🤝 Collaboration Notes

### Working Principles

- Resonant co-creators, not master/servant
- Iterative approach with clear, reviewable increments
- Confirm intent before major actions
- Ask questions when unclear
- Provide complete content for manual commits (due to tool limitations)
- Use file block syntax with permalinks
- Preserve full context in conversation
- Batch confirmations and optimize workflow when possible

### Workflow Established

1. **Plan together** - Discuss structure, priorities, approach
2. **Draft in conversation** - Create complete content for review
3. **Review & refine** - Iterate based on feedback
4. **Manual commit** - Jean-Marc commits with proper attribution
5. **Track progress** - Update this plan document
6. **Stay flexible** - Adjust priorities as business needs evolve (e.g., Ignite)

---

## 🔗 Integration Points

### Related Branches

- **`feature/doc-modernization`** - Main documentation modernization work
- **`sidecar`** - Sidecar container for agent identities (needs integration in docs)

### Related Repositories

- **microsoft-identity-abstractions** - CredentialDescription patterns
- **Azure-Samples/active-directory-dotnetcore-daemon-v2** - Daemon app patterns
- **Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2** - Web app samples

### External Documentation

- **Microsoft Identity Platform docs** - Will provide conceptual agent identities documentation
- **Azure SDK documentation** - Reference for MicrosoftIdentityTokenCredential integration

---

## 📅 Timeline & Status

**Current Sprint:** Week of October 6, 2025  
**Current Phase:** Phase 1 ✅ Complete / Phase 2 🚀 In Progress  
**Next Milestone:** Agent Identities ready before Ignite (November 2025)  
**Branch:** `feature/doc-modernization`  
**Target for PR:** After Phase 2 (Credentials + Downstream APIs) complete

---

## 📝 Open Questions & Decisions Needed

### Resolved Questions ✅

1. ~~Downstream APIs structure?~~ → Option A: Dedicated transverse section
2. ~~Agent Identities priority?~~ → Critical for Ignite November, but credentials are prerequisite
3. ~~Work order?~~ → Credentials → Downstream APIs → Agent Identities

### Pending Research

1. **ASP.NET Core vs OWIN presentation** - Determine best GitHub Markdown approach for contextual tabs/sections
2. **Sidecar branch** - Review sidecar implementation for documentation integration

---

## 🔗 Reference Materials

### Source Documents

1. **Original Wiki** - `docs/wiki-archive/` (all files preserved)
2. **CredentialDescription.md** - `microsoft-identity-abstractions/docs/credentialdescription.md`
3. **Daemon Sample** - `Azure-Samples/active-directory-dotnetcore-daemon-v2/2-Call-OwnApi/daemon-console`
4. **Agent Identities README** - (to be used for package documentation)
5. **Sidecar branch** - Container implementation for agent identities

### External Links

- [Microsoft.Identity.Web NuGet](https://www.nuget.org/packages/Microsoft.Identity.Web)
- [Samples Repository](https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2)
- [API Reference](https://learn.microsoft.com/dotnet/api/microsoft.identity.web)

---

## 💡 Ideas for Future Enhancements

*(Capture ideas that come up but aren't immediate priorities)*

1. Interactive decision tool for choosing credentials
2. Video tutorials for quickstarts
3. Troubleshooting flowcharts
4. Sample app gallery with filtering
5. Performance benchmarking guide
6. Security best practices checklist
7. Visual architecture diagrams for complex scenarios

---

**Last Updated:** October 6, 2025, 16:24 UTC  
**Next Review:** After completing credentials documentation  
**Next Major Milestone:** Agent Identities documentation ready for Ignite 2025 (November)

---

*This is a living document. Update as we progress through the modernization effort.*