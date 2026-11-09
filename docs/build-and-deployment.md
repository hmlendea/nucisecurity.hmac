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
NuciSecurity.HMAC.slnx
├── NuciSecurity.HMAC/
│   └── NuciSecurity.HMAC.csproj
├── NuciSecurity.HMAC.ArchitectureTests/
│   └── NuciSecurity.HMAC.ArchitectureTests.csproj
└── NuciSecurity.HMAC.UnitTests/
    └── NuciSecurity.HMAC.UnitTests.csproj
```

## Project Configuration

### NuciSecurity.HMAC.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NuciSecurity.HMAC</RootNamespace>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>

    <!-- Package metadata -->
    <Version>4.1.4</Version>
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
    <RootNamespace>NuciSecurity.HMAC.UnitTests</RootNamespace>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <ExcludeFromCodeCoverage>true</ExcludeFromCodeCoverage>
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

### NuciSecurity.HMAC.ArchitectureTests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>NuciSecurity.HMAC.ArchitectureTests</RootNamespace>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <ExcludeFromCodeCoverage>true</ExcludeFromCodeCoverage>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="NUnit" Version="4.5.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
    <PackageReference Include="TngTech.ArchUnitNET.NUnit" Version="0.13.5" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\NuciSecurity.HMAC\NuciSecurity.HMAC.csproj" />
  </ItemGroup>
</Project>
```

## CI/CD Pipeline

### GitHub Actions Workflows

#### Build and Test Workflow (`.github/workflows/dotnet.yml`)

```yaml
name: .NET

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]

jobs:
  build:
    name: Build
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 10.0.x
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore
    - name: Test
      run: dotnet test --no-build --verbosity normal
```

#### Security Scan Workflow (`.github/workflows/security.yml`)

```yaml
name: Security

on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]
  schedule:
    - cron: '0 0 * * 0'

jobs:
  security-scan:
    name: Security Scan
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      contents: read

    steps:
    - uses: actions/checkout@v7

    - name: Setup .NET
      uses: actions/setup-dotnet@v5
      with:
        dotnet-version: 10.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Check for vulnerable packages
      run: dotnet list package --vulnerable --include-transitive

    - name: Run CodeQL Analysis
      uses: github/codeql-action/init@v4
      with:
        languages: csharp

    - name: Build
      run: dotnet build --no-restore

    - name: Perform CodeQL Analysis
      uses: github/codeql-action/analyze@v4
```

The Security Scan workflow runs on:
- Every push to `master`
- Every pull request targeting `master`
- Weekly schedule (Sundays at 00:00 UTC)

It performs:
1. **Dependency vulnerability scanning** — Checks all direct and transitive NuGet dependencies for known vulnerabilities
2. **CodeQL static analysis** — Analyses the C# codebase for security issues

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