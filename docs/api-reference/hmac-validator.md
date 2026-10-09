# HmacValidator

**Namespace**: `NuciSecurity.HMAC`
**Type**: `static class`
**Assembly**: `NuciSecurity.HMAC.dll`

## Methods

### IsTokenValid\<TObject\>

```csharp
public static bool IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey) where TObject : class
```

Validates whether the provided token matches the generated token for the given object and secret.

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `expectedToken` | `string` | The expected HMAC token. Returns false if null, empty, or whitespace. |
| `obj` | `TObject` | The object to validate against. Must not be null. |
| `sharedSecretKey` | `string` | The shared secret key. Must not be null, empty, or whitespace. |

#### Returns

`bool` — True if the token matches, false otherwise.

#### Exceptions

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `obj` is null |
| `ArgumentNullException` | `sharedSecretKey` is null, empty, or whitespace |

#### Behaviour

1. If `expectedToken` is null, empty, or whitespace → return `false`
2. Validate `obj` and `sharedSecretKey` (throw if null)
3. Generate token via `HmacEncoder.GenerateToken(obj, sharedSecretKey)`
4. Compare using `string.Equals` (exact match)
5. Return comparison result

#### Example

```csharp
bool isValid = HmacValidator.IsTokenValid(receivedToken, payload, "shared-secret-key");
if (!isValid)
{
    // Handle invalid token
}
```

#### Flow Reference

See [Token Validation Flow](../flows/token-validation.md)

---

### Validate\<TObject\>

```csharp
public static void Validate<TObject>(string expectedToken, TObject obj, string sharedSecretKey) where TObject : class
```

Validates the token and throws an exception if invalid.

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `expectedToken` | `string` | The expected HMAC token |
| `obj` | `TObject` | The object to validate against |
| `sharedSecretKey` | `string` | The shared secret key |

#### Returns

`void`

#### Exceptions

| Exception | Condition |
|-----------|-----------|
| `SecurityException` | Token is not valid (message: "The HMAC token is not valid.") |
| `ArgumentNullException` | `obj` is null |
| `ArgumentNullException` | `sharedSecretKey` is null, empty, or whitespace |

#### Behaviour

1. Calls `IsTokenValid(expectedToken, obj, sharedSecretKey)`
2. If result is `false` → throw `SecurityException`
3. If result is `true` → return (no exception)

#### Example

```csharp
try
{
    HmacValidator.Validate(receivedToken, payload, "shared-secret-key");
    // Token is valid, proceed
}
catch (SecurityException)
{
    // Token invalid, reject request
}
```

#### Flow Reference

See [Token Validation Flow](../flows/token-validation.md)

---

## Validation Semantics

| Scenario | `IsTokenValid` | `Validate` |
|----------|----------------|------------|
| Token matches | `true` | No exception |
| Token mismatch | `false` | `SecurityException` |
| Token null/empty | `false` | `SecurityException` |
| Object null | `ArgumentNullException` | `ArgumentNullException` |
| Secret null/empty | `ArgumentNullException` | `ArgumentNullException` |

## Comparison Method

Validation uses `string.Equals` for exact string comparison. This is not constant-time, but is acceptable for HMAC validation where the attacker cannot observe timing differences in a meaningful way.

## Related

- [HmacEncoder](../api-reference/hmac-encoder.md)
- [Attributes](../components/attributes.md)
- [Data Model](../data-model.md)
- [Token Validation Flow](../flows/token-validation.md)