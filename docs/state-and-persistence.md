# State and Persistence

## Overview

NuciSecurity.HMAC is a **stateless library** with **no persistence** of its own. All state is managed by the caller.

## Library State

### No Internal State

```csharp
public static class HmacEncoder
{
    // No static fields with mutable state
    // No instance fields (static class)
    // All data passed as parameters
}

public static class HmacValidator
{
    // No static fields with mutable state
    // No instance fields (static class)
    // All data passed as parameters
}
```

### Reflection Cache (Read-Only)

```csharp
// Internal: Property info cached per-type (initialized once)
private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();
```

- **Initialized**: First access per type
- **Immutable**: Never modified after initialization
- **Thread-safe**: `ConcurrentDictionary` for concurrent access
- **No eviction**: Types don't change at runtime
- **Memory**: ~1 KB per type (negligible)

## Caller State Management

### Secret Key

**Responsibility**: Caller

```csharp
// Caller stores and manages secret
private readonly string _hmacSecret;

public PaymentService(IConfiguration config)
{
    _hmacSecret = config["Hmac:Secret"]
        ?? throw new InvalidOperationException("HMAC secret not configured");
}
```

**Storage Options**:
- Configuration (appsettings.json, environment variables)
- Secret vault (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
- In-memory (development only)

**Rotation**: Caller implements rotation logic

```csharp
public class RotatingHmacService
{
    private readonly List<string> _secrets;  // Current + previous

    public bool Validate(string token, object payload)
    {
        return _secrets.Any(s => HmacValidator.IsTokenValid(token, payload, s));
    }
}
```

### Object State

**Responsibility**: Caller

```csharp
// Caller creates and manages objects
var request = new PaymentRequest
{
    MerchantId = "MERCH_123",
    OrderId = "ORD_456",
    Amount = 99.99m
};

// Object must be stable during token generation/validation
// No mutation between GenerateToken and Validate
```

**Requirements**:
- Object must not be mutated between token generation and validation
- All non-ignored properties must have consistent values
- Collections must have stable enumeration order

### Token State

**Responsibility**: Caller

```csharp
// Caller transmits token with request
// No token storage in library
string token = HmacEncoder.GenerateToken(request, secret);
await SendAsync(token, request);

// Receiver validates immediately
bool valid = HmacValidator.IsTokenValid(receivedToken, receivedRequest, secret);
```

**No Token Storage**:
- Library does not store tokens
- No token database, cache, or registry
- Tokens are ephemeral (generated, transmitted, validated, discarded)

## Persistence Patterns

### Pattern 1: Request-Response (Stateless)

```
Client                          Server
  │                                │
  ├─ GenerateToken(request) ──────►│
  │                                │
  ├─ POST /api + token ───────────►│
  │                                ├─ IsTokenValid(token, request)
  │                                │
  │◄──── 200 OK ───────────────────┤
  │                                │
```

- No server-side state
- Token validated on receipt
- Scales horizontally

### Pattern 2: Token with Expiration (Application State)

```csharp
public class ExpiringRequest
{
    public string Data { get; set; }
    public DateTime ExpiresAt { get; set; }  // Included in token
    public string Nonce { get; set; }        // Included in token
}

// Server validates expiration
if (request.ExpiresAt < DateTime.UtcNow)
    return Unauthorized();

// Server tracks nonces (application state)
if (_nonceStore.Contains(request.Nonce))
    return Unauthorized();

_nonceStore.Add(request.Nonce);
```

- Expiration in object (included in token)
- Nonce tracking requires application storage
- Library remains stateless

### Pattern 3: Key Rotation (Application State)

```csharp
public class KeyRotationService
{
    private readonly ConcurrentDictionary<string, string> _keys = new();

    public void AddKey(string keyId, string secret)
    {
        _keys[keyId] = secret;
    }

    public bool Validate(string token, object payload, string keyId)
    {
        if (!_keys.TryGetValue(keyId, out var secret))
            return false;

        return HmacValidator.IsTokenValid(token, payload, secret);
    }
}
```

- Key storage is application responsibility
- Library validates with provided secret
- Rotation logic in application

## What the Library Does NOT Persist

| State | Library | Caller |
|-------|---------|--------|
| Secret keys | ❌ | ✅ |
| Generated tokens | ❌ | ✅ (if needed) |
| Validation results | ❌ | ✅ (if needed) |
| Nonces/timestamps | ❌ | ✅ (if needed) |
| Key rotation state | ❌ | ✅ |
| Rate limiting | ❌ | ✅ |
| Audit logs | ❌ | ✅ |

## Migration and Upgrades

### Library Upgrade

- No migration needed for library upgrade
- No persisted state to migrate
- Just replace DLL / update package

### Secret Rotation

```csharp
// 1. Add new secret (keep old)
_secrets.Add(newSecret);

// 2. Deploy new version to all servers

// 3. Update clients to use new secret

// 4. Remove old secret after grace period
_secrets.Remove(oldSecret);
```

### Token Format Change (Breaking)

- All existing tokens invalid
- Regenerate all tokens
- Coordinate with all parties

## Testing State

### Unit Tests (Stateless)

```csharp
[Test]
public void GenerateToken_Stateless()
{
    var obj = new TestDto { Value = "test" };
    string secret = "secret";

    // Multiple calls, same result
    string token1 = HmacEncoder.GenerateToken(obj, secret);
    string token2 = HmacEncoder.GenerateToken(obj, secret);

    Assert.That(token1, Is.EqualTo(token2));
}
```

### Integration Tests (Caller State)

```csharp
[Test]
public void FullFlow_WithSecretRotation()
{
    var service = new RotatingHmacService();
    service.AddKey("v1", "secret-v1");
    service.AddKey("v2", "secret-v2");

    var request = new PaymentRequest { Amount = 100m };

    // Token generated with v1
    string token = HmacEncoder.GenerateToken(request, "secret-v1");

    // Validated with v1 (still works)
    Assert.That(service.Validate(token, request), Is.True);

    // After rotation, v2 also works for new tokens
    string newToken = HmacEncoder.GenerateToken(request, "secret-v2");
    Assert.That(service.Validate(newToken, request), Is.True);
}
```

## Related

- [Architecture](../architecture.md)
- [Concurrency and Scheduling](../concurrency-and-scheduling.md)
- [Security Model](../security.md)
- [Change Guide](../change-guide.md)