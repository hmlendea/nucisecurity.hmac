# Build and Deployment

## Build System

### Toolchain

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 10.0+ | Build, test, pack |
| NuGet | 6.12+ | Package management |
| MSBuild | 17.12+ | Build engine |

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build NuciExtensions.sln

# Build specific project
dotnet build NuciSecurity.HMAC/NuciSecurity.HMAC.csproj

# Build with specific configuration
dotnet build -c Release
```

### Build Output

```
NuciSecurity.HMAC/bin/Debug/net10.0/
├── NuciSecurity.HMAC.dll
├── NuciSecurity.HMAC.pdb
├── NuciSecurity.HMAC.deps.json
├── NuciSecurity.HMAC.xml (documentation)
└── NuciExtensions.dll (copied)
```

## Solution Structure

```
NuciExtensions.sln
├── NuciSecurity.HMAC/
│   └── NuciSecurity.HMAC.csproj
└── NuciSecurity.HMAC.UnitTests/
    └── NuciSecurity.HMAC.UnitTests.csproj
```

## Project Configuration

### NuciSecurity.HMAC.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>

    <!-- Package metadata -->
    <Version>4.1.3</Version>
    <Description>Library for generating and validating deterministic HMAC tokens from object instances.</Description>
    <Authors>Horațiu Mlendea</Authors>
    <Copyright>Copyright 2026 © Horațiu Mlendea</Copyright>
    <RepositoryUrl>https://github.com/hmlendea/nucisecurity</RepositoryUrl>
    <PackageLicenseExpression>GPL-3.0-or-later</PackageLicenseExpression>
    <PackageTags>HMAC Authentication Security</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NuciExtensions" Version="5.3.1" />
  </ItemGroup>
</Project>
```

### NuciSecurity.HMAC.UnitTests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="NUnit" Version="4.5.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\NuciSecurity.HMAC\NuciSecurity.HMAC.csproj" />
  </ItemGroup>
</Project>
```

## CI/CD Pipeline

### GitHub Actions Workflow

```yaml
# .github/workflows/dotnet.yml
name: .NET Build and Test

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release
```

### Build Verification

```bash
# Local verification before push
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
```

## Packaging

### NuGet Package

```bash
# Create package
dotnet pack NuciSecurity.HMAC/NuciSecurity.HMAC.csproj -c Release -o ./artifacts

# Output
artifacts/
└── NuciSecurity.HMAC.4.1.3.nupkg
```

### Package Contents

```
NuciSecurity.HMAC.4.1.3.nupkg
├── lib/net10.0/
│   ├── NuciSecurity.HMAC.dll
│   └── NuciSecurity.HMAC.xml
├── NuciSecurity.HMAC.nuspec
└── [metadata]
```

### Publishing

```bash
# Publish to NuGet.org
dotnet nuget push artifacts/NuciSecurity.HMAC.4.1.3.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

## Versioning

### Scheme: Semantic Versioning (SemVer)

```
MAJOR.MINOR.PATCH
4.1.3
│ │ └── Patch: Bug fixes, no API changes
│ └── Minor: New features, backward compatible
└── Major: Breaking changes
```

### Version History

| Version | Date | Changes |
|---------|------|---------|
| 4.1.3 | 2026 | Current |
| 4.1.2 | 2025 | Bug fix |
| 4.1.1 | 2025 | Minor feature |
| 4.1.0 | 2025 | Major feature |

### Version Bumping

```bash
# Patch
dotnet pack /p:Version=4.1.4

# Minor
dotnet pack /p:Version=4.2.0

# Major
dotnet pack /p:Version=5.0.0
```

## Deployment Environments

### Development
- Local machine
- `dotnet build` / `dotnet test`
- Debug configuration

### CI/CD
- GitHub Actions
- Ubuntu latest
- Release configuration
- All tests must pass

### Production (NuGet)
- Published to NuGet.org
- Release configuration
- Signed packages (if configured)

## Build Artifacts

### Required for Deployment

| Artifact | Location | Purpose |
|----------|----------|---------|
| `.dll` | `bin/Release/net10.0/` | Runtime assembly |
| `.xml` | `bin/Release/net10.0/` | IntelliSense documentation |
| `.nupkg` | `artifacts/` | NuGet package |

### Not Deployed

| Artifact | Reason |
|----------|--------|
| `.pdb` | Debug symbols (optional) |
| `.deps.json` | Runtime dependency resolution |
| `obj/` | Build intermediates |

## Build Validation Checklist

- [ ] `dotnet restore` succeeds
- [ ] `dotnet build -c Release` succeeds (0 errors, 0 warnings)
- [ ] `dotnet test -c Release` passes (7/7)
- [ ] `dotnet pack -c Release` creates valid `.nupkg`
- [ ] Package installs correctly in test project
- [ ] Documentation XML generated

## Troubleshooting Build

### Common Issues

| Issue | Solution |
|-------|----------|
| `CS8632` nullable warning | Add `#nullable disable` or fix code |
| Package not found | Run `dotnet restore` |
| Test discovery fails | Ensure `NUnit3TestAdapter` installed |
| Version conflict | Check `PackageReference` versions |

### Clean Build

```bash
dotnet clean
rm -rf bin obj
dotnet restore
dotnet build
```

## Related

- [Configuration](../configuration.md)
- [Testing](../testing.md)
- [Dependencies](../dependencies.md)
- [Architecture](../architecture.md)