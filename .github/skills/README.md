# AI Coding Assistant Skills for Microsoft.Identity.Web

This folder contains **Skills** - specialized knowledge modules that help AI coding assistants provide better assistance for specific scenarios.

## What Are Skills?

Skills are an **open standard** for sharing domain-specific knowledge with AI coding assistants. They are markdown files with structured guidance that AI assistants use when helping with specific tasks. Unlike general instructions, skills are **scenario-specific** and activated when the assistant detects relevant context (keywords, file patterns, or explicit requests).

### Supported AI Assistants

Skills work with multiple AI coding assistants that support the open skills format:

- **GitHub Copilot** - Native support in VS Code, Visual Studio, GitHub Copilot CLI,  and other IDEs
- **Claude** (Anthropic) - Via Claude for VS Code extension and Claude Code
- **Other assistants** - Any AI tool that follows the skills convention

## Available Skills

| Skill | Description | Full Guide |
|-------|-------------|------------|
| [entra-id-aspire-authentication](./entra-id-aspire-authentication/SKILL.md) | Adding Microsoft Entra ID authentication to .NET Aspire applications | [Aspire Integration Guide](../../docs/frameworks/aspire.md) |
| [entra-id-aspire-provisioning](./entra-id-aspire-provisioning/SKILL.md) | Provisioning Entra ID app registrations for Aspire apps using Microsoft Graph PowerShell | [Aspire Integration Guide](../../docs/frameworks/aspire.md) |

> **💡 Tip:** Skills are condensed versions optimized for AI assistants. For comprehensive documentation with detailed explanations, diagrams, and troubleshooting, see the linked full guides.
>
> **🔄 Two-phase workflow:** Use the **authentication skill** first to add code (Phase 1), then the **provisioning skill** to create app registrations (Phase 2).

## How to Use Skills

### Option 1: Repository-Level (Recommended for Teams)

Copy the skill folder to your project's `.github/skills/` directory:

```
your-repo/
├── .github/
│   └── skills/
│       └── entra-id-aspire-authentication/
│           └── SKILL.md
```

Copilot will automatically use this skill when working in your repository.

### Option 2: User-Level (Personal Setup)

Install skills globally so they're available across all your projects:

**Windows:**
```powershell
# Create the skills directory
mkdir "$env:USERPROFILE\.github\skills\entra-id-aspire-authentication" -Force

# Copy the skill (or download from this repo)
Copy-Item "SKILL.md" "$env:USERPROFILE\.github\skills\entra-id-aspire-authentication\"
```

Location: `%USERPROFILE%\.github\skills\`

**Linux / macOS:**
```bash
# Create the skills directory
mkdir -p ~/.github/skills/entra-id-aspire-authentication

# Copy the skill (or download from this repo)
cp SKILL.md ~/.github/skills/entra-id-aspire-authentication/
```

Location: `~/.github/skills/`

### Option 3: Reference in Chat

You can also explicitly tell Copilot to use a skill:

> "Using the entra-id-aspire-authentication skill, add authentication to my Aspire app"

## Skill File Structure

Each skill follows this structure:

```markdown
---
name: skill-name
description: When Copilot should use this skill
license: MIT
---

# Skill Title

## When to Use This Skill
- Trigger condition 1
- Trigger condition 2

## Implementation Guide
...
```

The YAML frontmatter helps AI assistants understand when to apply the skill.

## Creating New Skills

1. Create a folder under `.github/skills/` with your skill name
2. Add a `SKILL.md` file with:
   - YAML frontmatter (`name`, `description`, `license`)
   - Clear "When to Use" section
   - Step-by-step implementation guidance
   - Code examples and configuration snippets
   - Troubleshooting tips

## Skills vs. Instructions

| Aspect | Instructions file | Skills |
|--------|-------------------|--------|
| Scope | Always active for the repo | Activated by context/keywords |
| Purpose | General coding standards | Specific implementation scenarios |
| Location | `.github/copilot-instructions.md` | `.github/skills/<name>/SKILL.md` |
| Content | Style guides, conventions | Step-by-step tutorials, patterns |
| Standard | Varies by AI assistant | Open standard across assistants |

## Resources

- [Microsoft.Identity.Web Documentation](../../docs/README.md)
- [Aspire Integration Guide](../../docs/frameworks/aspire.md)
- [GitHub copilot skills](https://docs.github.com/en/copilot/concepts/agents/about-agent-skills)
- [GitHub Copilot Documentation](https://docs.github.com/copilot)
