# Design Proposal: Unified CSS Architecture for Microsoft Identity Web Blazor Templates

## Summary

This proposal addresses the CSS duplication and inconsistencies across Microsoft Identity Web Blazor project templates by establishing a unified CSS architecture that promotes maintainability, consistency, and better developer experience.

## Motivation and goals

Currently, the Microsoft Identity Web repository contains multiple Blazor project templates with duplicated and inconsistent CSS implementations:

- **BlazorServerWeb-CSharp** uses `site.css` with specific styling patterns
- **ComponentsWebAssembly-CSharp** uses `app.css` with nearly identical styling but different file naming
- **Test applications** use various approaches including component-scoped CSS with different breakpoints

This duplication leads to:
- Maintenance overhead when updating styles across templates
- Inconsistent user experience across different project types
- Developer confusion about which styling approach to follow
- Potential styling bugs when templates diverge over time

The goal is to create a unified CSS architecture that:
- Eliminates code duplication across templates
- Provides consistent styling and behavior
- Establishes clear guidelines for Blazor styling in Identity Web projects
- Maintains backward compatibility while improving maintainability

## In scope

1. **CSS consolidation**: Create shared CSS modules that can be reused across all Blazor templates
2. **Standardized breakpoints**: Establish consistent responsive design breakpoints
3. **Naming conventions**: Define clear CSS file naming standards
4. **Component architecture**: Balance between global styles and component-scoped styles
5. **Template consistency**: Ensure all project templates use the same base styling approach
6. **Documentation**: Provide clear guidelines for CSS customization and extension

## Out of scope

- Complete visual redesign of the templates (maintaining current design language)
- Changes to non-Blazor templates (MVC, Web API, etc.)
- Third-party CSS framework integration (Bootstrap, Material, etc.)
- Dynamic theming or CSS-in-JS solutions
- Performance optimizations beyond basic consolidation

## Risks / unknowns

1. **Breaking changes**: Developers who have customized existing CSS files may need to update their implementations
2. **Template generation complexity**: Shared CSS approach may complicate template packaging and distribution
3. **Customization limitations**: Over-consolidation might reduce flexibility for specific template customizations
4. **Upgrade path**: Existing projects may face challenges when updating to new template versions
5. **Build process impact**: Shared CSS architecture may require changes to build and packaging processes

## Examples

### Current State Problems

**BlazorServerWeb template** (`site.css`):
```css
@media (max-width: 767.98px) {
    .main .top-row:not(.auth) {
        display: none;
    }
}
```

**ComponentsWebAssembly template** (`app.css`):
```css
@media (max-width: 767.98px) {
    .main .top-row:not(.auth) {
        display: none;
    }
}
```

**Test BlazorApp** (`MainLayout.razor.css`):
```css
@media (max-width: 640.98px) {
    .top-row {
        justify-content: space-between;
    }
}
```

### Proposed Solution

**Shared base CSS module** (`identity-web-blazor-base.css`):
```css
/* Microsoft Identity Web Blazor Base Styles */
/* Shared across all Blazor templates */

:root {
    --idweb-breakpoint-mobile: 640.98px;
    --idweb-breakpoint-tablet: 768px;
    --idweb-sidebar-width: 250px;
    --idweb-topbar-height: 3.5rem;
}

.idweb-layout {
    position: relative;
    display: flex;
    flex-direction: column;
}

.idweb-sidebar {
    background-image: linear-gradient(180deg, rgb(5, 39, 103) 0%, #3a0647 70%);
}

@media (max-width: 640.98px) {
    .idweb-layout .idweb-top-row:not(.auth) {
        display: none;
    }
}
```

**Template-specific CSS** (minimal overrides only):
```css
/* BlazorServerWeb specific overrides */
@import 'identity-web-blazor-base.css';

.custom-server-specific {
    /* Template-specific styles only */
}
```

**Component-scoped approach** (alternative):
```css
/* MainLayout.razor.css */
.layout {
    /* Uses CSS custom properties from base */
    width: var(--idweb-sidebar-width);
}
```

### Developer Experience Examples

**Before** (current duplication):
```
ProjectTemplates/
├── BlazorServerWeb-CSharp/wwwroot/css/site.css (186 lines)
├── ComponentsWebAssembly-CSharp/Client/wwwroot/css/app.css (186 lines, ~95% identical)
└── tests/DevApps/blazor/BlazorApp/Components/Layout/MainLayout.razor.css (98 lines, different approach)
```

**After** (unified architecture):
```
ProjectTemplates/
├── shared/css/
│   ├── identity-web-blazor-base.css (core styles)
│   ├── identity-web-components.css (reusable components)
│   └── identity-web-variables.css (CSS custom properties)
├── BlazorServerWeb-CSharp/wwwroot/css/site.css (imports + overrides only)
└── ComponentsWebAssembly-CSharp/Client/wwwroot/css/app.css (imports + overrides only)
```

## Detailed design

### CSS Architecture Structure

```
shared/css/
├── core/
│   ├── variables.css          # CSS custom properties, design tokens
│   ├── reset.css              # Minimal reset/normalize
│   ├── layout.css             # Core layout patterns (flexbox, grid)
│   └── typography.css         # Font families, sizes, weights
├── components/
│   ├── sidebar.css            # Sidebar navigation styles
│   ├── topbar.css             # Top navigation bar
│   ├── forms.css              # Form validation, inputs
│   └── blazor-error-ui.css    # Error UI component
└── themes/
    ├── default.css            # Default Microsoft Identity theme
    └── high-contrast.css      # Accessibility theme
```

### Implementation Strategy

1. **Phase 1: Extract common styles**
   - Analyze all existing CSS files
   - Extract shared patterns into base modules
   - Define CSS custom properties for design tokens

2. **Phase 2: Template migration**
   - Update BlazorServerWeb template to use shared CSS
   - Update ComponentsWebAssembly template to use shared CSS
   - Maintain existing class names for backward compatibility

3. **Phase 3: Component approach**
   - Evaluate component-scoped CSS for specific components
   - Create hybrid approach: shared base + component overrides
   - Update test applications to follow unified approach

4. **Phase 4: Documentation and guidelines**
   - Create styling guidelines for template customization
   - Document CSS architecture decisions
   - Provide migration guide for existing projects

### Backward Compatibility Strategy

- Maintain existing CSS class names
- Provide CSS imports that preserve current behavior
- Create migration guide with step-by-step instructions
- Support gradual adoption through template versioning

### CSS Custom Properties (Design Tokens)

```css
:root {
    /* Layout */
    --idweb-sidebar-width: 250px;
    --idweb-topbar-height: 3.5rem;
    --idweb-content-padding: 2rem;

    /* Breakpoints */
    --idweb-breakpoint-mobile: 640.98px;
    --idweb-breakpoint-tablet: 768px;
    --idweb-breakpoint-desktop: 1024px;

    /* Colors */
    --idweb-primary: #1b6ec2;
    --idweb-primary-dark: #1861ac;
    --idweb-sidebar-gradient-start: rgb(5, 39, 103);
    --idweb-sidebar-gradient-end: #3a0647;
    
    /* Typography */
    --idweb-font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
    --idweb-font-size-base: 0.9rem;
    --idweb-font-size-brand: 1.1rem;
}
```

## Drawbacks

1. **Initial complexity**: Introducing shared CSS architecture requires more upfront planning and coordination
2. **Template packaging**: Build process changes needed to include shared CSS in template packages
3. **Customization constraints**: Some flexibility may be lost in favor of consistency
4. **Migration effort**: Existing projects will need updates to adopt new architecture
5. **Testing overhead**: Changes require testing across all Blazor template variations

## Considered alternatives

### Alternative 1: Keep current duplication
- **Pros**: No breaking changes, simple maintenance
- **Cons**: Continued maintenance overhead, inconsistency growth over time

### Alternative 2: Single monolithic CSS file
- **Pros**: Simple import, no dependency management
- **Cons**: Larger bundle size, harder to customize specific components

### Alternative 3: CSS-in-JS or runtime styling
- **Pros**: Dynamic theming, component encapsulation
- **Cons**: Performance overhead, complexity, not aligned with current approach

### Alternative 4: Adopt external CSS framework
- **Pros**: Battle-tested, community support
- **Cons**: Dependency management, potential conflicts with Microsoft branding

### Alternative 5: Component-only approach (no shared CSS)
- **Pros**: Perfect encapsulation, no global conflicts
- **Cons**: Duplication at component level, harder to maintain design consistency

## Open questions

1. **Distribution mechanism**: How should shared CSS be packaged and distributed with templates?
2. **Versioning strategy**: How to handle CSS updates without breaking existing projects?
3. **Customization API**: What level of CSS customization should be supported through CSS custom properties?
4. **Build integration**: Should CSS processing be integrated into template build systems?
5. **Accessibility compliance**: How to ensure unified approach meets accessibility standards?
6. **Performance impact**: What is the bundle size impact of the shared approach vs. current duplication?

## References

- [CSS Architecture Guidelines](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_Architecture)
- [BEM Methodology](https://getbem.com/)
- [CSS Custom Properties](https://developer.mozilla.org/en-US/docs/Web/CSS/--*)
- [Responsive Design Breakpoints](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_Media_Queries/Using_media_queries)
- [ASP.NET Core Blazor CSS Isolation](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)