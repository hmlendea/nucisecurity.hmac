# Documentation Maintenance

## Overview

This document describes how to keep NuciSecurity.HMAC documentation current, accurate, and useful.

## Documentation Structure

```
docs/
├── INDEX.md                      # Main navigation
├── architecture.md               # System architecture
├── configuration.md              # Configuration guide
├── dependencies.md               # Dependencies
├── error-handling.md             # Error handling
├── security.md                   # Security model
├── testing.md                    # Testing strategy
├── build-and-deployment.md       # Build/deployment
├── logging.md                    # Logging practices
├── quick-start.md                # Getting started
├── troubleshooting.md            # Common issues
├── change-guide.md               # Safe modification guide
├── documentation-maintenance.md  # This file
├── faq.md                        # Frequently asked questions
├── ambiguities-and-open-questions.md
├── design-decisions.md           # Key decisions
├── concurrency-and-scheduling.md
├── state-and-persistence.md
├── integrations.md               # External integrations
├── repository-overview.md        # Repository purpose
├── repository-structure.md       # Source layout
├── data-model.md                 # Data structures
├── invariants.md                 # System invariants
├── api-usage-examples.md         # Usage examples
├── api-reference/
│   ├── INDEX.md
│   ├── hmac-encoder.md
│   └── hmac-validator.md
├── components/
│   ├── hmac-encoder.md
│   ├── hmac-validator.md
│   └── attributes.md
├── flows/
│   ├── token-generation.md
│   └── token-validation.md
└── behaviour/
    ├── token-generation.md
    └── token-validation.md
```

## Maintenance Principles

### 1. Documentation as Code

- Documentation lives in the repository
- Versioned with code
- Reviewed in PRs
- Tested via build (links, formatting)

### 2. Single Source of Truth

- Code is the ultimate reference
- Documentation reflects code, not vice versa
- Auto-generate where possible (XML docs → API reference)

### 3. Keep It Current

- Update docs with every code change
- No "TODO: update docs" comments
- If code changes, docs change in same PR

## Update Triggers

### Code Changes Requiring Doc Updates

| Code Change | Docs to Update |
|-------------|----------------|
| New public API | api-reference/, api-usage-examples.md |
| Changed behavior | behaviour/, flows/, components/ |
| New attribute | components/attributes.md, configuration.md |
| New exception | error-handling.md, api-reference/ |
| New dependency | dependencies.md |
| Config change | configuration.md |
| Security change | security.md |
| Test change | testing.md |
| Build change | build-and-deployment.md |

### Scheduled Reviews

| Frequency | Scope |
|-----------|-------|
| Every PR | Changed files only |
| Monthly | Quick-start, troubleshooting, FAQ |
| Quarterly | Architecture, design-decisions, security |
| Per release | All docs (version-specific) |

## Documentation Standards

### Markdown Style

```markdown
# Heading 1 (Page Title)

## Heading 2 (Major Section)

### Heading 3 (Subsection)

**Bold** for emphasis
`Code` for identifiers
```csharp
// Code blocks with language
```

| Table | With | Headers |
|-------|------|---------|

> **Note**: Callouts for important info

> ⚠️ **Warning**: Callouts for warnings
```

### Code Examples

- Must compile
- Use real types from library
- Include necessary `using` statements
- Show error handling

### Links

- Relative paths for repo files
- Absolute URLs for external
- Verify before committing

## Ownership

### Primary Maintainer

- **Horațiu Mlendea** — All documentation

### Contributors

- Anyone modifying code must update relevant docs
- PR reviewers verify doc updates

## Tools

### Validation

```bash
# Check for broken links
markdown-link-check docs/**/*.md

# Check formatting
markdownlint docs/**/*.md

# Spell check
cspell docs/**/*.md
```

### Generation

```bash
# Generate API reference from XML docs
dotnet build -c Release
# XML at bin/Release/net10.0/NuciSecurity.HMAC.xml
```

## Review Checklist

### For Every PR

- [ ] Code changes reflected in docs
- [ ] No broken links
- [ ] Code examples compile
- [ ] No outdated information
- [ ] Consistent formatting

### For Releases

- [ ] Version numbers updated
- [ ] Release notes linked
- [ ] Migration guide if breaking
- [ ] All examples tested

## Common Issues

### Stale Documentation

**Symptom**: Docs describe old behavior.

**Fix**: Update in same PR as code change.

### Missing Documentation

**Symptom**: New feature not documented.

**Fix**: Add docs before merging feature.

### Inconsistent Style

**Symptom**: Different formatting across files.

**Fix**: Apply style guide, use linter.

## Automation

### GitHub Actions

```yaml
# .github/workflows/docs.yml
name: Documentation Check

on: [pull_request]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Check links
        run: npx markdown-link-check docs/**/*.md
      - name: Lint markdown
        run: npx markdownlint docs/**/*.md
```

### Pre-commit Hook

```bash
# .pre-commit-config.yaml
repos:
  - repo: https://github.com/igorshubovych/markdownlint-cli
    rev: v0.40.0
    hooks:
      - id: markdownlint
        files: docs/
```

## Metrics

### Track

- Documentation coverage (public APIs documented)
- Link health (broken links)
- Freshness (last updated vs code changed)
- User feedback (issues, questions)

### Targets

- 100% public API documented
- 0 broken links
- < 30 days stale for core docs
- < 7 days stale for quick-start/FAQ

## Related

- [Architecture](../architecture.md)
- [Change Guide](../change-guide.md)
- [Build and Deployment](../build-and-deployment.md)