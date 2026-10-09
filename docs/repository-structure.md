# Repository Structure

## Source Tree

```
nucisecurity.hmac/
├── LICENSE
├── NuciExtensions.sln
├── README.md
├── SECURITY.md
├── docs/
│   ├── INDEX.md
│   ├── architecture.md
│   ├── configuration.md
│   ├── dependencies.md
│   ├── error-handling.md
│   ├── security.md
│   ├── testing.md
│   ├── build-and-deployment.md
│   ├── logging.md
│   ├── quick-start.md
│   ├── troubleshooting.md
│   ├── change-guide.md
│   ├── documentation-maintenance.md
│   ├── faq.md
│   ├── ambiguities-and-open-questions.md
│   ├── design-decisions.md
│   ├── concurrency-and-scheduling.md
│   ├── state-and-persistence.md
│   ├── integrations.md
│   ├── repository-overview.md
│   ├── repository-structure.md
│   ├── data-model.md
│   ├── invariants.md
│   ├── api-usage-examples.md
│   ├── api-reference/
│   │   ├── INDEX.md
│   │   ├── hmac-encoder.md
│   │   └── hmac-validator.md
│   ├── components/
│   │   ├── hmac-encoder.md
│   │   ├── hmac-validator.md
│   │   └── attributes.md
│   ├── flows/
│   │   ├── token-generation.md
│   │   └── token-validation.md
│   └── behaviour/
│       ├── token-generation.md
│       └── token-validation.md
├── NuciSecurity.HMAC/
│   ├── HmacEncoder.cs
│   ├── HmacIgnoreAttribute.cs
│   ├── HmacOrderAttribute.cs
│   ├── HmacValidator.cs
│   └── NuciSecurity.HMAC.csproj
└── NuciSecurity.HMAC.UnitTests/
    ├── HmacEncoderTests.cs
    ├── NuciSecurity.HMAC.UnitTests.csproj
    └── Helpers/
        ├── ObjectWithCollectionProperties.cs
        ├── ObjectWithDifferentOrderAttributes.cs
        ├── ObjectWithIgnoreAttributes.cs
        └── ObjectWithOrderAttributes.cs
```

## Module Organization

### NuciSecurity.HMAC (Main Library)

```
NuciSecurity.HMAC/
├── HmacEncoder.cs          # Core token generation
├── HmacValidator.cs        # Token validation
├── HmacIgnoreAttribute.cs  # [HmacIgnore] attribute
├── HmacOrderAttribute.cs   # [HmacOrder] attribute
└── NuciSecurity.HMAC.csproj
```

**Responsibilities**:
- `HmacEncoder`: Generate deterministic HMAC tokens
- `HmacValidator`: Validate tokens (boolean + exception)
- Attributes: Declarative property configuration

### NuciSecurity.HMAC.UnitTests (Test Project)

```
NuciSecurity.HMAC.UnitTests/
├── HmacEncoderTests.cs                    # 7 test cases
├── NuciSecurity.HMAC.UnitTests.csproj
└── Helpers/
    ├── ObjectWithCollectionProperties.cs
    ├── ObjectWithDifferentOrderAttributes.cs
    ├── ObjectWithIgnoreAttributes.cs
    └── ObjectWithOrderAttributes.cs
```

**Responsibilities**:
- Unit tests for all public API
- Test helper objects for attribute scenarios
- NUnit 4.5.1 test framework

## File Descriptions

### Core Library Files

| File | Purpose | Key Types |
|------|---------|-----------|
| `HmacEncoder.cs` | Token generation engine | `HmacEncoder.GenerateToken<T>()` |
| `HmacValidator.cs` | Token validation | `HmacValidator.IsTokenValid<T>()`, `Validate<T>()` |
| `HmacIgnoreAttribute.cs` | Exclude property from token | `[HmacIgnore]` |
| `HmacOrderAttribute.cs` | Control property order | `[HmacOrder(int)]` |

### Test Files

| File | Purpose |
|------|---------|
| `HmacEncoderTests.cs` | All 7 unit tests |
| `ObjectWithIgnoreAttributes.cs` | Test `[HmacIgnore]` |
| `ObjectWithOrderAttributes.cs` | Test `[HmacOrder]` |
| `ObjectWithCollectionProperties.cs` | Test collections |
| `ObjectWithDifferentOrderAttributes.cs` | Test order variations |

### Documentation Files

| Directory | Purpose |
|-----------|---------|
| `docs/` | Root documentation |
| `docs/api-reference/` | API reference (generated from XML) |
| `docs/components/` | Component deep-dives |
| `docs/flows/` | End-to-end flows |
| `docs/behaviour/` | Behavioural specifications |

## Build Artifacts (Generated)

```
NuciSecurity.HMAC/
├── bin/
│   └── Debug/
│       └── net10.0/
│           ├── NuciSecurity.HMAC.dll
│           ├── NuciSecurity.HMAC.pdb
│           ├── NuciSecurity.HMAC.deps.json
│           ├── NuciSecurity.HMAC.xml
│           └── NuciExtensions.dll
└── obj/
    └── Debug/
        └── net10.0/
            ├── NuciSecurity.HMAC.AssemblyInfo.cs
            ├── NuciSecurity.HMAC.csproj.FileListAbsolute.txt
            └── project.assets.json

NuciSecurity.HMAC.UnitTests/
├── bin/
│   └── Debug/
│       └── net10.0/
│           ├── NuciSecurity.HMAC.UnitTests.dll
│           ├── NuciSecurity.HMAC.UnitTests.pdb
│           ├── NuciSecurity.HMAC.UnitTests.deps.json
│           └── NuciSecurity.HMAC.UnitTests.runtimeconfig.json
└── obj/
    └── Debug/
        └── net10.0/
            └── (similar structure)
```

## Naming Conventions

### Projects
- `NuciSecurity.HMAC` - Main library
- `NuciSecurity.HMAC.UnitTests` - Unit tests

### Namespaces
- `NuciSecurity.HMAC` - All public types

### Files
- PascalCase for C# files: `HmacEncoder.cs`
- PascalCase for test files: `HmacEncoderTests.cs`
- kebab-case for markdown: `token-generation.md`

## Dependency Graph

```
NuciExtensions.sln
├── NuciSecurity.HMAC
│   └── NuciExtensions (5.3.1)  ← NuGet
└── NuciSecurity.HMAC.UnitTests
    ├── NuciSecurity.HMAC       ← ProjectReference
    ├── Microsoft.NET.Test.Sdk  ← NuGet
    ├── Moq                     ← NuGet
    ├── NUnit                   ← NuGet
    └── NUnit3TestAdapter       ← NuGet
```

## Git Ignore Patterns

```gitignore
# Build outputs
bin/
obj/

# IDE
.vs/
*.user
*.suo

# OS
.DS_Store
Thumbs.db

# NuGet
*.nupkg
packages/

# Test results
TestResults/
*.trx
```

## Related

- [Repository Overview](../repository-overview.md)
- [Architecture](../architecture.md)
- [Build and Deployment](../build-and-deployment.md)