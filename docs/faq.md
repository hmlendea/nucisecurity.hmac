# Frequently Asked Questions

## General

### What is NuciSecurity.HMAC?

A .NET library for generating and validating deterministic HMAC-SHA512 tokens from object instances. Used for request authentication between trusted parties.

### What .NET versions are supported?

.NET 10.0+ (net10.0 target framework).

### Is it thread-safe?

Yes. All methods are static and stateless. No shared mutable state.

### What license?

GPL-3.0-or-later.

## Token Generation

### Why are my tokens different each time?

Check for:
- Non-deterministic properties (DateTime.Now, Guid.NewGuid())
- Collections with unstable enumeration order
- Culture-dependent formatting (use InvariantCulture)

### Can I exclude properties from the token?

Yes, use `[HmacIgnore]` attribute:

```csharp
public class Request
{
    public string Included { get; set; }
    [HmacIgnore]
    public string Excluded { get; set; }
}
```

### Can I control property order?

Yes, use `[HmacOrder]` attribute:

```csharp
public class Request
{
    [HmacOrder(1)]
    public string First { get; set; }
    [HmacOrder(2)]
    public string Second { get; set; }
}
```

### How are collections handled?

Collections are enumerated in their natural order. For deterministic tokens, sort collections before token generation:

```csharp
request.Items = request.Items.OrderBy(x => x).ToArray();
```

## Token Validation

### Should I use IsTokenValid or Validate?

- **IsTokenValid**: Returns `bool`, no exception. Preferred for normal flow.
- **Validate**: Throws `SecurityException` on failure. Use for fail-fast.

### Why does validation fail with same object?

Common causes:
- Different secret key
- Property values differ (check all non-ignored properties)
- Property order differs (use `[HmacOrder]`)
- Collection order differs
- Culture formatting differs

### Can I validate without the original object?

No. You need the object instance to regenerate the token for comparison.

## Security

### Is the secret key in the token?

No. The token is an HMAC hash. The secret never leaves your application.

### Can tokens be replayed?

Yes, tokens are deterministic. Include a timestamp/nonce in your object:

```csharp
public class Request
{
    public string Data { get; set; }
    public DateTime Timestamp { get; set; }  // Include in token
    public string Nonce { get; set; }        // Include in token
}
```

### What if the secret is compromised?

Rotate the secret immediately. All existing tokens become invalid.

```csharp
// Support multiple secrets during rotation
var secrets = new[] { currentSecret, previousSecret };
bool isValid = secrets.Any(s => HmacValidator.IsTokenValid(token, obj, s));
```

### Is MD5 used for security?

No. MD5 is only used as a checksum in the token prefix for integrity checking, not authentication. The actual security is HMAC-SHA512.

## Configuration

### Where is the configuration file?

There is no configuration file. Configuration is via:
- Attributes on properties
- Method parameters (secret key)
- Project file (target framework)

### How do I set the secret in production?

Load from secure configuration:

```csharp
string secret = configuration["Hmac:Secret"]
    ?? throw new InvalidOperationException("HMAC secret not configured");
```

Use Azure Key Vault, AWS Secrets Manager, or similar.

## Errors

### ArgumentNullException: obj

You passed a null object. Ensure object is instantiated.

### ArgumentNullException: sharedSecretKey

Secret is null, empty, or whitespace. Validate at startup.

### SecurityException: The HMAC token is not valid

Token validation failed. Use `IsTokenValid` for boolean check instead.

## Testing

### How do I test token generation?

```csharp
[Test]
public void TestTokenGeneration()
{
    var request = new PaymentRequest { Amount = 100m };
    string token = HmacEncoder.GenerateToken(request, "test-secret");

    Assert.That(HmacValidator.IsTokenValid(token, request, "test-secret"), Is.True);
}
```

### How do I test validation failure?

```csharp
[Test]
public void TestInvalidToken()
{
    var request = new PaymentRequest { Amount = 100m };
    string token = HmacEncoder.GenerateToken(request, "secret");

    request.Amount = 200m;  // Tamper

    Assert.That(HmacValidator.IsTokenValid(token, request, "secret"), Is.False);
}
```

## Integration

### Can I use it with ASP.NET Core?

Yes. See [Quick Start](../quick-start.md) for middleware and attribute examples.

### Can I use it with gRPC?

Yes. Include token in metadata:

```csharp
var headers = new Metadata { { "x-hmac-token", token } };
await client.MethodAsync(request, headers);
```

### Does it work with JSON serialization?

Yes. The object is used directly (reflection), not serialized. Ensure property getters return consistent values.

## Performance

### Is it fast?

Yes. HMAC-SHA512 is hardware-accelerated on modern CPUs. Reflection is cached per-type.

### Does it allocate much?

Minimal. StringBuilder for string-for-signing, byte arrays for HMAC. No excessive allocations.

### Can I cache tokens?

Tokens are deterministic, so caching is unnecessary. Just regenerate.

## Troubleshooting

### Tokens differ between environments

Check:
- Line endings (normalize to `\n`)
- Culture (use InvariantCulture)
- Collection order (sort explicitly)
- Property order (use `[HmacOrder]`)

### Build fails with CS8632

Nullable context issue. Add `#nullable disable` or fix annotations.

### Tests not found

Ensure `NUnit3TestAdapter` package is installed.

## Migration

### Upgrading from older version

Check release notes for breaking changes. Token format changes require regeneration.

### Changing secret key

All existing tokens become invalid. Plan rotation:
1. Deploy with both old and new secrets
2. Migrate clients
3. Remove old secret

## Related

- [Quick Start](../quick-start.md)
- [Troubleshooting](../troubleshooting.md)
- [Security Model](../security.md)
- [Configuration](../configuration.md)