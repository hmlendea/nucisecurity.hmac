# Dependencies

## External Dependencies

### NuGet Packages

| Package | Version | Purpose | License |
|---------|---------|---------|---------|
| `NuciExtensions` | 5.3.1 | `Reverse()`, `InvertCase()` string extensions | GPL-3.0-or-later |
| `Microsoft.NET.Test.Sdk` | 18.3.0 | Test runner (test project only) | MIT |
| `Moq` | 4.20.72 | Mocking framework (test project only) | BSD-3-Clause |
| `NUnit` | 4.5.1 | Test framework (test project only) | MIT |
| `NUnit3TestAdapter` | 6.2.0 | NUnit adapter for VS (test project only) | MIT |

### Framework Dependencies

| Assembly | Purpose |
|----------|---------|
| `System.Security.Cryptography` | HMACSHA512, MD5 |
| `System.Reflection` | Property enumeration, attributes |
| `System.Text` | StringBuilder, Encoding |
| `System.Collections` | IEnumerable handling |
| `System.Linq` | Query operations |
| `System.Runtime` | Core types |

## Internal Dependencies

### Project References

```
NuciSecurity.HMAC.UnitTests
    └── NuciSecurity.HMAC (ProjectReference)
```

### Component Dependencies

```
HmacEncoder
    ├── HmacIgnoreAttribute (reflection)
    ├── HmacOrderAttribute (reflection)
    └── NuciExtensions (Reverse, InvertCase)

HmacValidator
    └── HmacEncoder (GenerateToken)

HmacIgnoreAttribute
    └── (none)

HmacOrderAttribute
    └── (none)
```

## Dependency Graph

```
┌─────────────────────────────────────────────────────────────────┐
│                      NuciSecurity.HMAC                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ HmacEncoder │  │HmacValidator│  │      Attributes         │  │
│  └──────┬──────┘  └──────┬──────┘  └─────────────────────────┘  │
│         │                │                                       │
│         └────────┬───────┘                                       │
│                  ▼                                               │
│         ┌─────────────────┐                                      │
│         │  NuciExtensions │                                      │
│         │  Reverse()      │                                      │
│         │  InvertCase()   │                                      │
│         └─────────────────┘                                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    .NET Runtime (net10.0)                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ Cryptography    │  │ Reflection      │  │ Text/Collections│  │
│  │ HMACSHA512      │  │ PropertyInfo    │  │ StringBuilder   │  │
│  │ MD5             │  │ CustomAttribute │  │ Encoding        │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## NuciExtensions Details

**Package**: `NuciExtensions` 5.3.1
**Source**: https://github.com/hmlendea/nuciextensions
**Author**: Horațiu Mlendea
**License**: GPL-3.0-or-later

### Used Members

| Member | Namespace | Purpose |
|--------|-----------|---------|
| `StringExtensions.Reverse()` | `NuciExtensions` | Reverse string characters |
| `StringCasingExtensions.InvertCase()` | `NuciExtensions` | Invert character case |

### Version Constraint

```xml
<PackageReference Include="NuciExtensions" Version="5.3.1" />
```

**Note**: Version specified without range → exact version 5.3.1 required.

## Test Project Dependencies

### NuciSecurity.HMAC.UnitTests.csproj

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="NUnit" Version="4.5.1" />
  <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\NuciSecurity.HMAC\NuciSecurity.HMAC.csproj" />
</ItemGroup>
```

## Transitive Dependencies

### NuciExtensions 5.3.1 Dependencies
- None (targets net10.0 with no dependencies)

### Test Dependencies (Transitive)
- NUnit → NUnit.Engine, NUnit.Common
- Moq → Castle.Core
- Microsoft.NET.Test.Sdk → Multiple test platform packages

## Version Compatibility

| Component | Target Framework | Compatible With |
|-----------|------------------|-----------------|
| NuciSecurity.HMAC | net10.0 | .NET 10.0+ |
| NuciExtensions | net10.0 | .NET 10.0+ |
| Test Project | net10.0 | .NET 10.0+ |

## Dependency Management

### Adding Dependencies
1. Add to appropriate `.csproj` via `PackageReference`
2. Run `dotnet restore`
3. Update documentation

### Updating Dependencies
1. Check for updates: `dotnet list package --outdated`
2. Update version in `.csproj`
3. Run `dotnet restore`
4. Run tests: `dotnet test`
5. Update documentation

### Security Scanning
- Run `dotnet list package --vulnerable --include-transitive`
- Review advisories for each dependency
- Update vulnerable packages promptly

## License Compliance

| Package | License | Compatible with GPL-3.0? |
|---------|---------|-------------------------|
| NuciExtensions | GPL-3.0-or-later | Yes (same license) |
| Microsoft.NET.Test.Sdk | MIT | Yes |
| Moq | BSD-3-Clause | Yes |
| NUnit | MIT | Yes |
| NUnit3TestAdapter | MIT | Yes |

**All dependencies compatible with GPL-3.0-or-later**.

## Related

- [Architecture](../architecture.md)
- [Build and Deployment](../build-and-deployment.md)
- [NuciSecurity.HMAC.csproj](../NuciSecurity.HMAC/NuciSecurity.HMAC.csproj)