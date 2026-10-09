# Change Guide

This guide explains how to safely modify common areas of NuciSecurity.HMAC.

## ⚠️ Breaking Changes

**Any change to token generation logic is a breaking change.** Existing tokens will become invalid.

| Change | Breaking? | Notes |
|--------|-----------|-------|
| Add `[HmacIgnore]` to property | ✅ Yes | Property excluded from token |
| Remove `[HmacIgnore]` from property | ✅ Yes | Property now included |
| Change `[HmacOrder]` value | ✅ Yes | Changes property order |
| Change static salt | ✅ Yes | Changes all tokens |
| Change HMAC algorithm | ✅ Yes | Changes all tokens |
| Change string encoding | ✅ Yes | Changes all tokens |
| Add new property | ✅ Yes | New property included |
| Remove property | ✅ Yes | Property no longer in token |

## Safe Changes

| Change | Breaking? | Notes |
|--------|-----------|-------|
| Add `[HmacOrder]` to new property | ✅ Yes | New property included |
| Add new property without attributes | ✅ Yes | New property included |
| Change property type (same string repr) | ⚠️ Maybe | If string representation changes |
| Add logging | ❌ No | No token logic change |
| Add null checks | ❌ No | Defensive, no behavior change |
| Refactor method internals | ❌ No | If output unchanged |

## Modifying Token Generation

### Adding a New Property

```csharp
public class PaymentRequest
{
    public string MerchantId { get; set; }
    public string OrderId { get; set; }
    public decimal Amount { get; set; }

    // NEW: Will be included in token automatically
    public string Currency { get; set; } = "USD"
}
```

**Impact**: All existing tokens become invalid.

**Migration**:
1. Deploy new version
2. Regenerate all tokens
3. Communicate to clients

### Excluding a Property

```csharp
public class PaymentRequest
{
    public string MerchantId { get; set; }

    // NEW: Exclude from token
    [HmacIgnore]
    public string InternalNotes { get; set; }
}
```

**Impact**: Tokens generated before and after will differ if `InternalNotes` was previously included.

### Changing Property Order

```csharp
public class PaymentRequest
{
    // Move to first position
    [HmacOrder(1)]
    public string MerchantId { get; set; }

    [HmacOrder(2)]
    public string OrderId { get; set; }

    [HmacOrder(3)]
    public decimal Amount { get; set; }
}
```

**Impact**: Tokens change if order differs from previous.

## Modifying Validation

### Adding Pre-Validation

```csharp
public static bool IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey)
{
    // NEW: Early exit for empty tokens
    if (string.IsNullOrWhiteSpace(expectedToken))
        return false;

    // Existing validation
    return GenerateToken(obj, sharedSecretKey).Equals(expectedToken);
}
```

**Impact**: None (empty tokens were already invalid).

### Changing Exception Type

```csharp
// Before
throw new SecurityException("The HMAC token is not valid.");

// After
throw new UnauthorizedAccessException("The HMAC token is not valid.");
```

**Impact**: Breaking change for callers catching `SecurityException`.

## Modifying Attributes

### HmacIgnoreAttribute

```csharp
// Current
[AttributeUsage(AttributeTargets.Property)]
public class HmacIgnoreAttribute : Attribute { }

// Safe to add properties
public class HmacIgnoreAttribute : Attribute
{
    public string Reason { get; set; }  // NEW: Documentation only
}
```

**Impact**: None (new property, existing usage unchanged).

### HmacOrderAttribute

```csharp
// Current
public class HmacOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}

// Safe to add methods
public class HmacOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;

    public static int GetDefaultOrder() => int.MaxValue;  // NEW
}
```

**Impact**: None (additive change).

## Modifying Dependencies

### Updating NuciExtensions

```xml
<!-- Before -->
<PackageReference Include="NuciExtensions" Version="5.3.1" />

<!-- After -->
<PackageReference Include="NuciExtensions" Version="5.4.0" />
```

**Check**: Verify `Reverse()` and `InvertCase()` behavior unchanged.

### Adding New Dependency

```xml
<ItemGroup>
  <PackageReference Include="NuciExtensions" Version="5.3.1" />
  <PackageReference Include="NewLibrary" Version="1.0.0" />  <!-- NEW -->
</ItemGroup>
```

**Impact**: Review license compatibility (must be GPL-compatible).

## Testing Changes

### Before Any Change

```bash
dotnet test  # Must pass (7/7)
```

### After Change

```bash
dotnet build  # Must succeed
dotnet test   # Must pass (7/7)
```

### Adding New Tests

```csharp
[Test]
public void GenerateToken_NewProperty_IncludedInToken()
{
    var obj1 = new PaymentRequest { Amount = 100m, Currency = "USD" };
    var obj2 = new PaymentRequest { Amount = 100m, Currency = "EUR" };

    string token1 = HmacEncoder.GenerateToken(obj1, "secret");
    string token2 = HmacEncoder.GenerateToken(obj2, "secret");

    Assert.That(token1, Is.Not.EqualTo(token2));
}
```

## Version Bumping

### Patch (Bug Fix)

```xml
<Version>4.1.4</Version>  <!-- 4.1.3 → 4.1.4 -->
```

- No API changes
- No token format changes
- Backward compatible

### Minor (New Feature)

```xml
<Version>4.2.0</Version>  <!-- 4.1.3 → 4.2.0 -->
```

- New API (backward compatible)
- May include new token-affecting features (breaking)

### Major (Breaking Change)

```xml
<Version>5.0.0</Version>  <!-- 4.1.3 → 5.0.0 -->
```

- Breaking API changes
- Token format changes
- Requires migration

## Migration Checklist

### For Token-Affecting Changes

- [ ] Update version number
- [ ] Document breaking change in release notes
- [ ] Provide migration guide
- [ ] Regenerate all tokens
- [ ] Communicate to all clients
- [ ] Update tests
- [ ] Update documentation

### For Non-Token Changes

- [ ] Update version number (patch/minor)
- [ ] Update tests
- [ ] Update documentation
- [ ] Run full test suite

## Review Checklist

Before merging any change:

- [ ] Does it change token generation? → Breaking change
- [ ] Does it change validation logic? → Test thoroughly
- [ ] Does it add/remove properties? → Breaking change
- [ ] Does it change attribute behavior? → Breaking change
- [ ] Does it change dependencies? → Check licenses
- [ ] Are all 7 tests passing? → Required
- [ ] Is documentation updated? → Required
- [ ] Is version bumped appropriately? → Required

## Related

- [Architecture](../architecture.md)
- [Testing](../testing.md)
- [Security Model](../security.md)
- [Build and Deployment](../build-and-deployment.md)