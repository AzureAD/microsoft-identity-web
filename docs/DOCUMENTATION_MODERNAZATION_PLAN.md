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
| Calling Downstream APIs | Week of Oct 6 | ✅ Complete (1 file pending) |
| Sidecar Documentation | Before Ignite Nov 2025 | ✅ Complete |
| Agent Identities Documentation | Before Ignite Nov 2025 | ✅ Complete |
| Credentials Documentation | Week of Oct 27 | 🚀 High Priority |
| Core Scenarios (Web Apps, APIs, Daemon) | TBD | 📝 TODO |

---

## 📐 Documentation Structure (Current State)

```
docs/
├── README.md                          ✅ COMPLETE - Main home page
├── getting-started/
│   ├── quickstart-webapp.md           ✅ COMPLETE - Updated with explicit auth schemes
│   ├── quickstart-webapi.md           ✅ COMPLETE - Updated with explicit auth schemes
│   └── why-microsoft-identity-web.md  📝 TODO
├── calling-downstream-apis/           ✅ COMPLETE (5 of 6 files done)
│   ├── README.md                      ✅ COMPLETE - Overview, patterns, IDownstreamApi
│   ├── from-web-apps.md               📝 MISSING - Web app specific aspects
│   ├── from-web-apis.md               ✅ COMPLETE - Web API specific aspects (OBO)
│   ├── microsoft-graph.md             ✅ COMPLETE - GraphServiceClient integration
│   ├── azure-sdks.md                  ✅ COMPLETE - MicrosoftIdentityTokenCredential
│   └── custom-apis.md                 ✅ COMPLETE - IAuthorizationHeaderProvider
├── sidecar/                           ✅ COMPLETE - Comprehensive sidecar documentation
│   ├── index.md                       ✅ COMPLETE - Main index
│   ├── README.md                      ✅ COMPLETE - Overview
│   ├── agent-identities.md            ✅ COMPLETE - Agent identities integration
│   ├── configuration.md               ✅ COMPLETE - Configuration guide
│   ├── endpoints.md                   ✅ COMPLETE - API endpoints
│   ├── installation.md                ✅ COMPLETE - Setup instructions
│   ├── comparison.md                  ✅ COMPLETE - Comparison with alternatives
│   ├── security.md                    ✅ COMPLETE - Security considerations
│   ├── troubleshooting.md             ✅ COMPLETE - Troubleshooting guide
│   ├── faq.md                         ✅ COMPLETE - Frequently asked questions
│   ├── scenarios/                     ✅ COMPLETE - Scenario examples
│   └── toc.yaml                       ✅ COMPLETE - Table of contents
├── authentication/
│   ├── credentials/                   🚀 HIGH PRIORITY - Top remaining gap
│   │   ├── README.md                  ✅ COMPLETE - Moved from Credentials.md
│   │   ├── certificateless.md         ✅ EXISTS - FIC+MSI deep dive
│   │   ├── certificates.md            ✅ EXISTS - All cert types
│   │   ├── client-secrets.md          ✅ EXISTS - Dev/test only
│   │   └── token-decryption.md        ✅ EXISTS - Special case
│   └── token-cache/                   📝 TODO
│       ├── README.md
│       ├── serialization.md
│       ├── distributed-cache.md
│       └── troubleshooting.md
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
│   └── azure-functions/               📝 TODO
│       └── README.md
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
├── blog-posts/                        ✅ EXISTS - Preserved content
├── design/                            ✅ EXISTS - Design documents
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

**2. Web App Quickstart (docs/getting-started/quickstart-webapp.md)** ✅
- Updated to use explicit authentication scheme pattern
- `.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp()`
- .NET 9 code examples
- Complete app registration setup guide

**3. Web API Quickstart (docs/getting-started/quickstart-webapi.md)** ✅
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

### Phase 2: Calling Downstream APIs (October 6-27, 2025)

**Transverse Documentation - SUBSTANTIALLY COMPLETE** ✅

**1. docs/calling-downstream-apis/README.md** ✅
- Main overview of downstream API patterns
- Token acquisition approaches
- `IDownstreamApi` and `IAuthorizationHeaderProvider` overview
- When to use each approach
- Cross-references to scenarios

**2. docs/calling-downstream-apis/from-web-apis.md** ✅
- On-Behalf-Of (OBO) flow documentation
- Long-running processes with OBO
- Token caching considerations
- Error handling specific to APIs
- ASP.NET Core examples

**3. docs/calling-downstream-apis/microsoft-graph.md** ✅
- `GraphServiceClient` integration guide
- Using `AddMicrosoftGraph()` extensions
- Delegated permissions vs app permissions
- Common Graph API patterns
- Batching and paging

**4. docs/calling-downstream-apis/azure-sdks.md** ✅
- **MicrosoftIdentityTokenCredential from Microsoft.Identity.Web.Azure**
- Integrating with Azure SDK clients (Storage, KeyVault, etc.)
- Configuration patterns
- Code examples for common Azure services
- Managed Identity integration

**5. docs/calling-downstream-apis/custom-apis.md** ✅
- Calling your own protected APIs
- Configuring BaseUrl, scopes, relative paths
- Using `IAuthorizationHeaderProvider` for custom HTTP logic
- Adding custom headers
- Handling API-specific authentication patterns

**6. docs/calling-downstream-apis/from-web-apps.md** 📝 MISSING
- Acquiring tokens on behalf of signed-in user
- Incremental consent
- Handling token acquisition failures
- ASP.NET Core examples
- OWIN examples (if different)

**Status:** 5 of 6 files complete (83% done). `from-web-apps.md` remains as a gap.

### Phase 3: Sidecar & Agent Identities (October 2025)

**Sidecar Documentation - COMPLETE** ✅

The sidecar documentation is comprehensive and production-ready:

**1. docs/sidecar/index.md** ✅ - Main entry point
**2. docs/sidecar/README.md** ✅ - Overview and introduction
**3. docs/sidecar/agent-identities.md** ✅ - Agent identities integration
**4. docs/sidecar/installation.md** ✅ - Setup and installation guide
**5. docs/sidecar/configuration.md** ✅ - Configuration reference
**6. docs/sidecar/endpoints.md** ✅ - API endpoints documentation
**7. docs/sidecar/comparison.md** ✅ - Comparison with alternatives
**8. docs/sidecar/security.md** ✅ - Security considerations
**9. docs/sidecar/troubleshooting.md** ✅ - Troubleshooting guide
**10. docs/sidecar/faq.md** ✅ - Frequently asked questions
**11. docs/sidecar/scenarios/** ✅ - Scenario examples folder
**12. docs/sidecar/toc.yaml** ✅ - Table of contents for navigation

**Achievement:** This represents a **major milestone** - the sidecar and agent identities documentation is ready for Ignite 2025 (November)! 🎯

---

## � Documentation Health & Broken Links

### Broken Links Audit (October 27, 2025) ✅ COMPLETED

**Critical Issues Identified and Fixed:**

1. **✅ FIXED - Filename Mismatches in docs/README.md**
   - `quickstart-web-app.md` → corrected to `quickstart-webapp.md`
   - `quickstart-web-api.md` → corrected to `quickstart-webapi.md`
   - **Impact:** Main documentation page now has working quickstart links
   - **Effort:** 2 minutes

2. **✅ FIXED - Credentials File Structure**
   - Moved `authentication/Credentials.md` → `authentication/credentials/README.md`
   - Properly integrates with credential subfiles (certificateless.md, certificates.md, etc.)
   - **Status:** Structure now correct, ready for credential file creation
   - **Action Taken:** File moved and all references updated

3. **📝 KNOWN - Missing calling-downstream-apis/from-web-apps.md**
   - Already tracked in plan as 1 remaining file for Phase 2
   - Multiple files link to it
   - **Action:** Create file as part of completing Phase 2

4. **📝 TRACKED - Missing Folders (Lower Priority)**
   - All documented in plan as TODO sections:
     - `scenarios/` (web-apps, web-apis, daemon, azure-functions)
     - `packages/`
     - `advanced/`
     - `deployment/`
     - `migration/`
   - **Action:** Create as part of planned phases

### Link Health Strategy Going Forward

**Completed Actions:**
- ✅ Fixed immediate broken links in main README
- ✅ Corrected credentials documentation structure
- ✅ Verified quickstart file paths

**Next Steps:**
- Run broken link check before completing each phase
- Ensure new docs don't reference unplanned future docs
- Use relative links consistently
- Consider adding link checker to CI/CD when docs are more complete

---

## �🚀 Current Sprint: Week of October 27, 2025

### Revised Priorities Based on Actual Progress

**Priority 1: Credentials Documentation** 🚀 HIGH PRIORITY

**Status:** This is now the **primary remaining gap** in the documentation modernization. With downstream APIs substantially complete and sidecar/agent identities documentation finished, credentials documentation is the critical missing piece.

**Why This Matters:**
- Credentials are fundamental to all scenarios (web apps, web APIs, daemon apps, agent identities)
- Users need guidance on choosing the right authentication approach
- Prerequisite for understanding certificateless auth, managed identity, and production deployment
- Referenced throughout other documentation sections

#### Files to Create:

~~1. **docs/authentication/credentials/README.md** (Main hub - 20-30 min read)~~ ✅ **COMPLETE**
   - ✅ Overview of authentication approaches
   - ✅ Decision flow chart (Mermaid diagram)
   - ✅ Comparison table (when to use what)
   - ✅ Quick start examples for all credential types
   - ✅ Links to detailed guides
   - ✅ Security best practices
   - **Status:** Moved from authentication/Credentials.md, properly structured

**Remaining Files:**

The credentials folder already contains the detailed guide files. They may need review/updates:

2. **docs/authentication/credentials/certificateless.md** ✅ EXISTS
   - Review for completeness and consistency with README

3. **docs/authentication/credentials/certificates.md** ✅ EXISTS
   - Review for completeness and consistency with README

4. **docs/authentication/credentials/client-secrets.md** ✅ EXISTS
   - Review for completeness and consistency with README

5. **docs/authentication/credentials/token-decryption.md** ✅ EXISTS
   - Review for completeness and consistency with README

**Next Action:** Review existing credential files to ensure they align with the comprehensive README.md hub document.

**Content Source:**
- Adapt credentialdescription.md from microsoft-identity-abstractions
- Add Microsoft.Identity.Web-specific patterns
- Include ASP.NET Core and OWIN examples

---

**Priority 2: Complete Calling Downstream APIs** 📝 MINOR GAP

**Status:** Only 1 file missing to complete this section

#### File to Create:

**docs/calling-downstream-apis/from-web-apps.md**
- Acquiring tokens on behalf of signed-in user
- Incremental consent patterns
- Handling token acquisition failures
- ASP.NET Core examples
- OWIN examples (if applicable)
- Error handling and troubleshooting

**Effort:** Low - Single file, well-defined scope

---

## 📝 Future Priorities (After Credentials Documentation)

### Phase 4: Core Scenario Deep Dives

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

**Azure Functions Scenario**
- Configuration patterns
- Authentication in serverless
- Best practices

### Phase 5: Package Documentation

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

### Phase 6: Advanced Topics

1. Multiple Authentication Schemes (include explicit scheme explanation)
2. Incremental Consent & Conditional Access
3. Long-Running Processes (detailed OBO patterns)
4. APIs Behind Gateways (Azure API Management, Front Door)
5. Performance Optimization
6. Logging & Diagnostics
7. Customization
8. Token Cache (serialization, distributed cache, troubleshooting)

### Phase 7: Deployment & Migration

**Deployment Guides:**
1. Azure App Service deployment
2. Container deployment
3. Behind proxies

**Migration Guides:**
1. v3 to v4 migration guide (for upcoming 4.0.0 release)
2. Update existing v1→v2 and v2→v3 guides if needed
3. Wiki → New Docs Mapping Document

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

### Content Priorities (Updated)

**This Week (Critical Path):**
1. ✅ **COMPLETED** - Fixed quickstart filename references in docs/README.md (2 min)
2. ✅ **COMPLETED** - Moved Credentials.md to credentials/README.md (proper structure)
3. 🚀 **IN PROGRESS** - Credentials documentation - Primary remaining gap (5 files to create)

**Quick Win:**
2. 📝 **Complete calling-downstream-apis/from-web-apps.md** - Single file to finish section

**High Priority (After Core Gaps Filled):**
3. 📝 Core scenario deep dives (Web Apps, Web APIs, Daemon)
4. 📝 Package documentation (Azure, AgentIdentities, etc.)

**Medium Priority:**
5. 📝 Advanced topics (auth schemes, long-running, gateways, incremental consent)
6. 📝 Token cache documentation

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

1. **Credentials Decision Flow** (in credentials/README.md) - HIGH PRIORITY
   - Flowchart helping developers choose authentication method
   - Based on: Azure? Production? Rotation needed? etc.

2. **Downstream API Call Flow** (in calling-downstream-apis/README.md)
   - Token acquisition and caching flow
   - Different patterns (delegated, OBO, app-only)
   - May already exist in current documentation

3. **Agent Identities Architecture** (in sidecar documentation)
   - How agent identities work
   - Sidecar container integration
   - Token flow for agents
   - Likely already exists in sidecar docs

4. **Documentation Navigation** (optional - in main README.md)
   - Visual map of documentation structure

---

## 📊 Success Metrics

How we'll know this modernization is successful:

1. **Completeness** - All high-priority sections documented ✅ **MOSTLY ACHIEVED**
   - ✅ Foundation complete
   - ✅ Downstream APIs 83% complete
   - ✅ Sidecar/Agent Identities complete
   - 🚀 Credentials documentation in progress

2. **Accuracy** - Code examples work with .NET 8/9 and v3.14.1+ ✅ **ACHIEVED**

3. **Discoverability** - Users can find what they need quickly ✅ **GOOD PROGRESS**
   - Clear structure in place
   - Navigation needs (credentials docs will significantly improve this)

4. **Maintainability** - Version-controlled, easy to update ✅ **ACHIEVED**

5. **Migration clarity** - Clear path from old wiki to new docs 🔄 **IN PROGRESS**
   - Wiki archive preserved
   - New structure established
   - Mapping document still needed

6. **Ignite readiness** - Agent identities documentation ready for public presentation ✅ **ACHIEVED**
   - Comprehensive sidecar documentation complete
   - Agent identities integration documented
   - Ready for November 2025 Ignite

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
- **`sidecar`** - Sidecar container for agent identities (now integrated in docs)

### Related Repositories

- **microsoft-identity-abstractions** - CredentialDescription patterns
- **Azure-Samples/active-directory-dotnetcore-daemon-v2** - Daemon app patterns
- **Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2** - Web app samples

### External Documentation

- **Microsoft Identity Platform docs** - Provides conceptual agent identities documentation
- **Azure SDK documentation** - Reference for MicrosoftIdentityTokenCredential integration

---

## 📅 Timeline & Status

**Current Sprint:** Week of October 27, 2025
**Current Phase:** Phase 3 ✅ Complete / Phase 4 🚀 In Progress
**Next Milestone:** Credentials documentation complete
**Branch:** `feature/doc-modernization`
**Target for PR:** After credentials documentation complete

### Progress Summary

**Completion Status:**
- ✅ **Phase 1 (Foundation):** 100% complete
- ✅ **Phase 2 (Downstream APIs):** 83% complete (5 of 6 files)
- ✅ **Phase 3 (Sidecar/Agent Identities):** 100% complete
- 🚀 **Phase 4 (Credentials):** 0% complete - **HIGH PRIORITY**
- 📝 **Phase 5 (Core Scenarios):** 0% complete
- 📝 **Phase 6+ (Advanced, Deployment, Migration):** 0% complete

**Overall Documentation Modernization Progress:** ~40% complete

**Key Achievement:** Ignite 2025 (November) readiness ✅ - Sidecar and agent identities documentation is production-ready!

---

## 📝 Open Questions & Decisions Needed

### Resolved Questions ✅

1. ~~Downstream APIs structure?~~ → Option A: Dedicated transverse section ✅
2. ~~Agent Identities priority?~~ → Critical for Ignite November, achieved ✅
3. ~~Work order?~~ → Credentials → Downstream APIs → Agent Identities (revised based on actual progress)
4. ~~Sidecar branch integration?~~ → Complete and integrated ✅

### New Questions Based on Progress

1. **Credentials documentation status?** - README.md complete ✅, need to review existing detail files
2. **from-web-apps.md priority?** - Complete as part of Phase 2 finalization
3. **Next scenario priority after credentials review?** - Web Apps, Web APIs, or Daemon first?

### Pending Research

1. **ASP.NET Core vs OWIN presentation** - Determine best GitHub Markdown approach for contextual tabs/sections
2. **Migration mapping document** - Create wiki-to-new-docs mapping for users

---

## 🔗 Reference Materials

### Source Documents

1. **Original Wiki** - `docs/wiki-archive/` (all files preserved)
2. **CredentialDescription.md** - `microsoft-identity-abstractions/docs/credentialdescription.md`
3. **Daemon Sample** - `Azure-Samples/active-directory-dotnetcore-daemon-v2/2-Call-OwnApi/daemon-console`
4. **Sidecar Documentation** - `docs/sidecar/` (comprehensive set now complete)
5. **Completed Downstream APIs Docs** - `docs/calling-downstream-apis/` (reference for patterns)

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
8. Community contribution guide for documentation

---

## 🎉 Major Achievements to Date

1. ✅ **Foundation Complete** - README and quickstarts modernized with .NET 9
2. ✅ **Downstream APIs Substantially Complete** - 5 of 6 comprehensive guides done
3. ✅ **Sidecar Documentation Complete** - 12 comprehensive files covering all aspects
4. ✅ **Agent Identities Ready for Ignite 2025** - Production-ready documentation
5. ✅ **Clear Structure Established** - Scalable organization for future content
6. ✅ **Modern Code Patterns** - Explicit authentication schemes, .NET 8/9 focus

---

**Last Updated:** October 27, 2025, 23:50 UTC
**Updated By:** Jean-Marc Prieur (@jmprieur), GitHub Copilot
**Recent Changes:**
- ✅ Fixed broken links (quickstart filenames)
- ✅ Moved Credentials.md to credentials/README.md
- ✅ Added Documentation Health section
- 📝 Updated credentials documentation status (README complete, detail files exist)
**Next Review:** After reviewing existing credential detail files
**Next Major Milestone:** Complete Phase 2 (from-web-apps.md) and finalize credentials documentation

---

*This is a living document. Update as we progress through the modernization effort.*
