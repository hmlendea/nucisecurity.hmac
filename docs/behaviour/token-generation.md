# Token Generation Behaviour

## Overview

Token generation creates a deterministic HMAC token from an object instance and a shared secret key. The token can be transmitted alongside the object to verify integrity and authenticity.

## Basic Usage

```csharp
using NuciSecurity.HMAC;

string sharedSecret = "super-secret-key";

var payload = new PaymentRequest
{
    MerchantId = "merchant-42",
    Amount = 125.50m,
    Currency = "EUR",
    NonSignedMetadata = "ignore-me"
};

string token = HmacEncoder.GenerateToken(payload, sharedSecret);
// token: "nWVxAwKhNBл0bbQBLHSatlFgoahLhFCl7A3SGCjJd7lzCLyguP8qY3LeB4dADxH3PLIudyC0O83kAbлm6w0jrqaa"
```

## Property Inclusion Rules

### Included by Default
- All **public instance properties** of the object
- Properties of any type (primitives, complex objects, collections)

### Excluded via `[HmacIgnore]`
```csharp
public class PaymentRequest
{
    public string MerchantId { get; set; }      // Included
    public decimal Amount { get; set; }         // Included

    [HmacIgnore]
    public string InternalNotes { get; set; }   // EXCLUDED

    [HmacIgnore]
    public DateTime CreatedAt { get; set; }     // EXCLUDED
}
```

### Ordered via `[HmacOrder]`
```csharp
public class PaymentRequest
{
    [HmacOrder(1)]
    public string MerchantId { get; set; }      // 1st

    [HmacOrder(2)]
    public decimal Amount { get; set; }         // 2nd

    [HmacOrder(3)]
    public string Currency { get; set; }        // 3rd

    public string ExtraField { get; set; }      // 4th (alphabetical after ordered)
}
```

**Order Rules**:
1. Properties with `[HmacOrder]` → sorted by `Order` ascending
2. Properties without `[HmacOrder]` → sorted by name alphabetically
3. `[HmacIgnore]` properties → completely excluded (order irrelevant)

## Value Normalization

| Type | Format | Example |
|------|--------|---------|
| `DateTime` | Round-trip ISO 8601 (`"O"`) | `2026-10-09T12:30:45.1234567Z` |
| `bool` | Lowercase | `true` / `false` |
| `null` | Internal marker | `|#EmptyValue#|` |
| `string` | As-is | `"hello"` |
| Numbers | `ToString()` | `125.50` |
| Enums | `ToString()` | `"Pending"` |
| Custom objects | Recursive (in collections) | See below |

## Collections

### Complex Object Collections
```csharp
public class Order
{
    public string OrderId { get; set; }
    public List<OrderItem> Items { get; set; }  // List of complex objects
}

public class OrderItem
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }

    [HmacIgnore]
    public string InternalNote { get; set; }
}
```
- Each item processed **recursively** via `GetStringForSigning`
- Null items → `EmptyValue` marker
- Item properties follow same inclusion/ordering rules

### Scalar Collections
```csharp
public class Product
{
    public string Name { get; set; }
    public List<string> Tags { get; set; }      // List of strings
    public List<int> Scores { get; set; }       // List of ints
}
```
- Each element converted to string (`ToString()`)
- Null elements → `EmptyValue` marker
- Joined with `FieldSeparator` (`|#FieldSeparator#|`)

## Determinism Guarantees

**Same object + same secret = same token** (always)

```csharp
var obj1 = new PaymentRequest { MerchantId = "A", Amount = 100 };
var obj2 = new PaymentRequest { MerchantId = "A", Amount = 100 };

string token1 = HmacEncoder.GenerateToken(obj1, "secret");
string token2 = HmacEncoder.GenerateToken(obj2, "secret");

token1 == token2  // true
```

**Factors that change token**:
- Any included property value change
- Property order change (via `[HmacOrder]` or alphabetical)
- Adding/removing `[HmacIgnore]`
- Changing secret key
- Collection element order change

## Token Format

The generated token is a **Base64-like string** with:
- **No `=` padding** (padded to avoid)
- **Cyrillic substitutions**: `/`→`Л`, `+`→`л`
- **Case inverted** (uppercase↔lowercase)
- **Fixed length**: ~88 characters (for SHA512)

Example: `nWVxAwKhNBл0bbQBLHSatlFgoahLhFCl7A3SGCjJd7lzCLyguP8qY3LeB4dADxH3PLIudyC0O83kAbлm6w0jrqaa`

## Error Handling

| Input | Behavior |
|-------|----------|
| `obj` = null | `ArgumentNullException` |
| `sharedSecretKey` = null | `ArgumentNullException` |
| `sharedSecretKey` = "" | `ArgumentNullException` |
| `sharedSecretKey` = "   " | `ArgumentNullException` |

## Thread Safety

**Thread-safe** — no shared state, pure function of inputs.

## Performance

- **Reflection overhead**: Property enumeration on each call
- **String allocation**: StringBuilder for string-for-signing
- **Crypto**: Single HMAC-SHA512 computation
- **Typical**: <1ms for small objects

## Common Patterns

### API Request Signing
```csharp
// Client side
var request = new ApiRequest { ... };
string token = HmacEncoder.GenerateToken(request, sharedSecret);
httpClient.DefaultRequestHeaders.Add("X-HMAC-Token", token);

// Server side
bool valid = HmacValidator.IsTokenValid(request.Headers["X-HMAC-Token"], request, sharedSecret);
```

### Webhook Verification
```csharp
// Webhook receiver
public IActionResult Receive(WebhookPayload payload, string signature)
{
    if (!HmacValidator.IsTokenValid(signature, payload, webhookSecret))
        return Unauthorized();

    // Process payload
}
```

### DTO Anti-Tampering
```csharp
// Serialize with token
var dto = new DataDto { ... };
string token = HmacEncoder.GenerateToken(dto, secret);
var envelope = new { Data = dto, Token = token };

// Deserialize and verify
var received = JsonSerializer.Deserialize<Envelope>(json);
if (!HmacValidator.IsTokenValid(received.Token, received.Data, secret))
    throw new SecurityException("Data tampered");
```

## Best Practices

1. **Use constants for secret** — Don't hardcode in source
2. **Rotate secrets periodically** — Plan for key rotation
3. **Use `[HmacOrder]` explicitly** — Avoids alphabetical surprises
4. **Ignore volatile fields** — Timestamps, internal IDs, computed properties
5. **Test token stability** — Verify same input produces same token
6. **Don't log tokens** — Treat as sensitive credentials

## Anti-Patterns

```csharp
// BAD: Including timestamp (changes every call)
public class BadRequest
{
    public string Data { get; set; }
    public DateTime Timestamp { get; set; }  // Token changes every time!
}

// GOOD: Ignore timestamp
public class GoodRequest
{
    public string Data { get; set; }

    [HmacIgnore]
    public DateTime Timestamp { get; set; }  // Excluded from token
}

// BAD: Relying on alphabetical order
public class BadOrder
{
    public string AField { get; set; }
    public string BField { get; set; }  // Order depends on names
}

// GOOD: Explicit order
public class GoodOrder
{
    [HmacOrder(1)]
    public string AField { get; set; }

    [HmacOrder(2)]
    public string BField { get; set; }  // Order explicit
}
```

## Related

- [Token Generation Flow](../flows/token-generation.md)
- [HmacEncoder API](../api-reference/hmac-encoder.md)
- [Attributes](../components/attributes.md)
- [Data Model](../data-model.md)