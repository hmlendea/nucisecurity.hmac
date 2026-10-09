# Configuration

## Overview

NuciSecurity.HMAC has **no external configuration files**. All configuration is done via:
1. **Code** — Attributes on properties
2. **Method parameters** — Secret key passed at runtime
3. **Compile-time** — Target framework in csproj

## Configuration Sources

| Source | Scope | Example |
|--------|-------|---------|
| Attributes | Per-property | `[HmacOrder(1)]`, `[HmacIgnore]` |
| Method parameters | Per-call | `GenerateToken(obj, "secret")` |
| Project file | Build-time | `<TargetFramework>net10.0</TargetFramework>` |

## Attribute Configuration

### HmacIgnoreAttribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class HmacIgnoreAttribute : Attribute { }
```

**Configuration**: Presence/absence on property
- **Present** → Property excluded from token
- **Absent** → Property included (default)

### HmacOrderAttribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class HmacOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
```

**Configuration**: `order` parameter (int)
- **Any int valid**: Negative, zero, positive
- **Default**: `int.MaxValue` (if attribute absent)
- **Lower values**: Processed first

## Runtime Configuration

### Secret Key

The shared secret key is passed as a method parameter:

```csharp
string secret = GetSecretFromSecureStore();  // Your secret management
string token = HmacEncoder.GenerateToken(payload, secret);
```

**Requirements**:
- Must not be null
- Must not be empty
- Must not be whitespace only
- Should be cryptographically random
- Should be stored securely (not in code)

### Object Instance

The object to sign is passed as a method parameter:

```csharp
var payload = new PaymentRequest { ... };
string token = HmacEncoder.GenerateToken(payload, secret);
```

**Requirements**:
- Must not be null
- Must be a class (reference type)
- Public instance properties used for token

## Build Configuration

### Target Framework

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

**Current**: net10.0
**Supported**: .NET 10.0+

### Package Metadata

```xml
<PropertyGroup>
  <Version>4.1.3</Version>
  <Description>Library for generating and validating deterministic HMAC tokens from object instances.</Description>
  <Authors>Horațiu Mlendea</Authors>
  <Copyright>Copyright 2026 © Horațiu Mlendea</Copyright>
  <RepositoryUrl>https://github.com/hmlendea/nucisecurity</RepositoryUrl>
  <PackageLicenseExpression>GPL-3.0-or-later</PackageLicenseExpression>
  <PackageTags>HMAC Authentication Security</PackageTags>
</PropertyGroup>
```

### Package Reference

```xml
<ItemGroup>
  <PackageReference Include="NuciExtensions" Version="5.3.1" />
</ItemGroup>
```

## Environment-Specific Configuration

### Development
```csharp
// Use constant for testing
const string DevSecret = "dev-secret-key-for-testing-only";
```

### Production
```csharp
// Load from secure configuration
string secret = configuration["Hmac:Secret"]
    ?? throw new InvalidOperationException("HMAC secret not configured");
```

### CI/CD
```yaml
# GitHub Actions
env:
  HMAC_SECRET: ${{ secrets.HMAC_SECRET }}
```

## Configuration Validation

### At Runtime

```csharp
// HmacEncoder.GenerateToken validates:
ArgumentNullException.ThrowIfNull(obj);
ArgumentNullException.ThrowIfNullOrWhiteSpace(sharedSecretKey);

// HmacValidator validates:
if (string.IsNullOrWhiteSpace(expectedToken)) return false;
ArgumentNullException.ThrowIfNull(obj);
ArgumentNullException.ThrowIfNullOrWhiteSpace(sharedSecretKey);
```

### At Build Time

- Target framework validated by SDK
- Package references validated by NuGet
- Nullable reference types: Warning only (CS8632)

## Precedence

Not applicable — no layered configuration. Each call is independent.

## Migration

### Changing Secret

```csharp
// Old tokens become invalid immediately
// Regenerate all tokens with new secret
string newSecret = "new-secret";
string newToken = HmacEncoder.GenerateToken(payload, newSecret);
```

### Changing Attributes

```csharp
// Adding [HmacIgnore] to existing property
// → Breaking change: all existing tokens invalid

// Changing [HmacOrder] value
// → Breaking change if order changes relative to other properties
```

## Related

- [Attributes](../components/attributes.md)
- [Data Model](../data-model.md)
- [Build and Deployment](../build-and-deployment.md)