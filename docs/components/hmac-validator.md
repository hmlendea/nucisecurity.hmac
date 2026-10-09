# HmacValidator Component

**File**: `NuciSecurity.HMAC/HmacValidator.cs`
**Type**: `static class`
**Purpose**: Token validation (boolean and exception-based)

## Public API

| Method | Signature | Purpose |
|--------|-----------|---------|
| `IsTokenValid` | `IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey)` | Boolean validation |
| `Validate` | `Validate<TObject>(string expectedToken, TObject obj, string sharedSecretKey)` | Exception-throwing validation |

## Implementation

```csharp
public static class HmacValidator
{
    public static void Validate<TObject>(string expectedToken, TObject obj, string sharedSecretKey) where TObject : class
    {
        if (!IsTokenValid(expectedToken, obj, sharedSecretKey))
        {
            throw new SecurityException("The HMAC token is not valid.");
        }
    }

    public static bool IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey) where TObject : class
    {
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            return false;
        }

        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(sharedSecretKey);

        return HmacEncoder.GenerateToken(obj, sharedSecretKey).Equals(expectedToken);
    }
}
```

## Validation Logic

### IsTokenValid

1. **Early exit**: If `expectedToken` is null/empty/whitespace → return `false`
2. **Validate inputs**: Throw `ArgumentNullException` if `obj` is null or `sharedSecretKey` is null/empty/whitespace
3. **Generate token**: Call `HmacEncoder.GenerateToken(obj, sharedSecretKey)`
4. **Compare**: Use `string.Equals` for exact match
5. **Return**: Boolean result

### Validate

1. **Delegate**: Call `IsTokenValid(expectedToken, obj, sharedSecretKey)`
2. **Check result**: If `false` → throw `SecurityException("The HMAC token is not valid.")`
3. **Success**: If `true` → return normally

## Error Handling

| Scenario | `IsTokenValid` | `Validate` |
|----------|----------------|------------|
| Valid token | `true` | No exception |
| Invalid token | `false` | `SecurityException` |
| Token null/empty/whitespace | `false` | `SecurityException` |
| Object null | `ArgumentNullException` | `ArgumentNullException` |
| Secret null/empty/whitespace | `ArgumentNullException` | `ArgumentNullException` |

## SecurityException

- **Type**: `System.Security.SecurityException`
- **Message**: `"The HMAC token is not valid."`
- **Thrown by**: `Validate` when token invalid
- **Not thrown by**: `IsTokenValid` (returns `false` instead)

## Comparison Method

```csharp
return HmacEncoder.GenerateToken(obj, sharedSecretKey).Equals(expectedToken);
```

- Uses `string.Equals` (ordinal, case-sensitive)
- **Not constant-time** — acceptable for HMAC validation
- Exact match required (no trimming, no normalization)

## Dependencies

### Internal
- `HmacEncoder.GenerateToken` — Token generation for comparison

### External
- `System.Security.SecurityException` — Exception type for `Validate`
- `System.ArgumentNullException` — Input validation

## Thread Safety

All methods are **thread-safe** — no shared mutable state, pure functions of inputs.

## Test Coverage

No direct tests for `HmacValidator` — validation tested indirectly through `HmacEncoderTests`:
- Valid tokens return `true` / no exception
- Invalid tokens return `false` / throw `SecurityException`
- Null/empty token handling

## Modification Impact

| Change | Affected Areas |
|--------|----------------|
| Comparison method | Validation strictness, timing characteristics |
| Exception type/message | Consumer error handling |
| Null token handling | API contract |
| Input validation | Error semantics |

**Breaking**: Changing exception type or message breaks consumer error handling.