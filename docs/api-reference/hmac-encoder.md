# HmacEncoder

**Namespace**: `NuciSecurity.HMAC`
**Type**: `static class`
**Assembly**: `NuciSecurity.HMAC.dll`

## Methods

### GenerateToken\<TObject\>

```csharp
public static string GenerateToken<TObject>(TObject obj, string sharedSecretKey) where TObject : class
```

Generates a deterministic HMAC token from an object instance using a shared secret key.

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `obj` | `TObject` | The object to generate a token for. Must not be null. |
| `sharedSecretKey` | `string` | The shared secret key. Must not be null, empty, or whitespace. |

#### Returns

`string` — Base64-encoded HMAC token with Cyrillic substitutions (`/`→`Л`, `+`→`л`) and case inversion applied.

#### Exceptions

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | `obj` is null |
| `ArgumentNullException` | `sharedSecretKey` is null, empty, or whitespace |

#### Example

```csharp
using NuciSecurity.HMAC;

var payload = new PaymentRequest
{
    MerchantId = "merchant-42",
    Amount = 125.50m,
    Currency = "EUR",
    NonSignedMetadata = "ignore-me"
};

string token = HmacEncoder.GenerateToken(payload, "super-secret-key");
// Result: "nWVxAwKhNBл0bbQBLHSatlFgoahLhFCl7A3SGCjJd7lzCLyguP8qY3LeB4dADxH3PLIudyC0O83kAbлm6w0jrqaa"
```

#### Behaviour

1. Validates inputs (throws on null object or secret)
2. Constructs string-for-signing from object properties
3. Builds prefix with length and MD5 checksum
4. Computes HMAC-SHA512 with salted, reversed string
5. Applies Base64 encoding with Cyrillic substitutions
6. Applies case inversion
7. Returns final token

#### Flow Reference

See [Token Generation Flow](../flows/token-generation.md)

---

### IsTokenValid\<TObject\> [Obsolete]

```csharp
[Obsolete("Use HmacValidator.IsTokenValid instead.")]
public static bool IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey) where TObject : class
```

Validates a token by delegating to `HmacValidator.IsTokenValid`.

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `expectedToken` | `string` | The token to validate |
| `obj` | `TObject` | The object to validate against |
| `sharedSecretKey` | `string` | The shared secret key |

#### Returns

`bool` — True if valid, false otherwise.

#### Note

**Obsolete**. Use `HmacValidator.IsTokenValid` instead. This method is retained for backward compatibility and will be removed in a future major version.

#### Flow Reference

See [Token Validation Flow](../flows/token-validation.md)

---

## Usage Patterns

### Basic Token Generation

```csharp
string secret = "shared-secret";
var dto = new MyDto { Id = 1, Name = "Test" };
string token = HmacEncoder.GenerateToken(dto, secret);
```

### With Attributes

```csharp
public class PaymentRequest
{
    [HmacOrder(1)]
    public string MerchantId { get; set; }

    [HmacOrder(2)]
    public decimal Amount { get; set; }

    [HmacOrder(3)]
    public string Currency { get; set; }

    [HmacIgnore]
    public string InternalNotes { get; set; }  // Excluded from token
}
```

### Collections

```csharp
public class Order
{
    public string OrderId { get; set; }
    public List<OrderItem> Items { get; set; }  // Complex collection - recursive
    public List<string> Tags { get; set; }      // Scalar collection - flattened
}
```

---

## Related

- [HmacValidator](../api-reference/hmac-validator.md)
- [Attributes](../components/attributes.md)
- [Data Model](../data-model.md)
- [Token Generation Flow](../flows/token-generation.md)