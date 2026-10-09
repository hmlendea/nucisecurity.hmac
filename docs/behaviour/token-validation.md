# Token Validation Behaviour

## Overview

Token validation verifies that a received HMAC token matches the expected token for a given object and secret. Two APIs are provided: boolean (`IsTokenValid`) and exception-based (`Validate`).

## Basic Usage

### Boolean Validation
```csharp
using NuciSecurity.HMAC;

string sharedSecret = "super-secret-key";
string receivedToken = GetTokenFromRequest();  // e.g., from HTTP header

bool isValid = HmacValidator.IsTokenValid(receivedToken, payload, sharedSecret);

if (!isValid)
{
    // Reject request
    return Unauthorized();
}
```

### Exception-Based Validation
```csharp
try
{
    HmacValidator.Validate(receivedToken, payload, sharedSecret);
    // Token is valid, proceed
}
catch (SecurityException)
{
    // Token invalid, reject request
    return Unauthorized();
}
```

## Validation Semantics

### How Validation Works

Validation is performed by **regenerating** the token and comparing:

```
1. Generate token from received object + secret
2. Compare generated token with received token
3. Match → valid, Mismatch → invalid
```

This means:
- **No separate validation algorithm** — uses same generation logic
- **Guaranteed consistency** — validation always matches generation
- **Deterministic** — same inputs always produce same result

### Comparison Method

```csharp
generatedToken.Equals(expectedToken)
```

- **Ordinal comparison**: Exact byte-by-byte match
- **Case-sensitive**: Token case matters (due to `InvertCase`)
- **No trimming**: Whitespace is significant
- **No normalization**: No culture-specific transformations

## API Comparison

| Aspect | `IsTokenValid` | `Validate` |
|--------|----------------|------------|
| Return type | `bool` | `void` |
| Invalid token | Returns `false` | Throws `SecurityException` |
| Null token | Returns `false` | Throws `SecurityException` |
| Null object | Throws `ArgumentNullException` | Throws `ArgumentNullException` |
| Null secret | Throws `ArgumentNullException` | Throws `ArgumentNullException` |
| Use case | Conditional logic | Fail-fast |

## Error Handling Matrix

| Scenario | `IsTokenValid` | `Validate` |
|----------|----------------|------------|
| Valid token | `true` | No exception |
| Invalid token | `false` | `SecurityException` |
| Token null | `false` | `SecurityException` |
| Token empty | `false` | `SecurityException` |
| Token whitespace | `false` | `SecurityException` |
| Object null | `ArgumentNullException` | `ArgumentNullException` |
| Secret null | `ArgumentNullException` | `ArgumentNullException` |
| Secret empty | `ArgumentNullException` | `ArgumentNullException` |
| Secret whitespace | `ArgumentNullException` | `ArgumentNullException` |

## SecurityException

- **Type**: `System.Security.SecurityException`
- **Message**: `"The HMAC token is not valid."`
- **Namespace**: `System.Security`
- **Purpose**: Signal authentication failure

```csharp
try
{
    HmacValidator.Validate(token, payload, secret);
}
catch (SecurityException ex)
{
    // ex.Message == "The HMAC token is not valid."
    // Handle unauthorized access
}
```

## Common Patterns

### HTTP Middleware
```csharp
public class HmacValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _secret;

    public HmacValidationMiddleware(RequestDelegate next, string secret)
    {
        _next = next;
        _secret = secret;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string token = context.Request.Headers["X-HMAC-Token"];

        // Read body for validation
        var body = await ReadBodyAsync(context.Request);
        var payload = JsonSerializer.Deserialize<MyPayload>(body);

        if (!HmacValidator.IsTokenValid(token, payload, _secret))
        {
            context.Response.StatusCode = 401;
            return;
        }

        await _next(context);
    }
}
```

### Webhook Handler
```csharp
[HttpPost]
public IActionResult Receive([FromBody] WebhookPayload payload,
                             [FromHeader] string X_HMAC_Token)
{
    try
    {
        HmacValidator.Validate(X_HMAC_Token, payload, _webhookSecret);
    }
    catch (SecurityException)
    {
        return Unauthorized();
    }

    // Process verified webhook
    ProcessWebhook(payload);
    return Ok();
}
```

### Retry Logic
```csharp
public bool TryValidateWithRetry(string token, object payload, string secret, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        if (HmacValidator.IsTokenValid(token, payload, secret))
            return true;

        // Optional: log retry attempt
        Thread.Sleep(100);
    }
    return false;
}
```

## Timing Considerations

- **Not constant-time**: `string.Equals` short-circuits on first difference
- **Acceptable for HMAC**: Attacker already knows the token (they generated it)
- **No timing attack vector**: Token is not secret during validation

## Thread Safety

**Thread-safe** — no shared state, pure function of inputs.

## Performance

- **Regeneration cost**: Same as `GenerateToken`
- **Comparison cost**: O(n) string comparison
- **Typical**: <1ms for small objects
- **Optimization**: Cache generated tokens if validating same object multiple times

## Best Practices

1. **Use `IsTokenValid` for conditional logic** — Avoid exception overhead
2. **Use `Validate` for fail-fast** — When invalid token should stop execution
3. **Validate before processing** — Never process unvalidated data
4. **Don't catch `SecurityException` broadly** — Handle specifically
5. **Log validation failures** — For security monitoring (without logging tokens)
6. **Use same secret for generation and validation** — Obviously

## Anti-Patterns

```csharp
// BAD: Catching too broadly
try
{
    HmacValidator.Validate(token, payload, secret);
}
catch (Exception)  // Too broad!
{
    // Hides ArgumentNullException, etc.
}

// GOOD: Catch specific exception
try
{
    HmacValidator.Validate(token, payload, secret);
}
catch (SecurityException)
{
    // Only catches validation failures
}

// BAD: Ignoring validation result
HmacValidator.IsTokenValid(token, payload, secret);  // Result ignored!
ProcessPayload(payload);  // Processes unvalidated data!

// GOOD: Check result
if (!HmacValidator.IsTokenValid(token, payload, secret))
    throw new SecurityException("Invalid token");
ProcessPayload(payload);
```

## Related

- [Token Validation Flow](../flows/token-validation.md)
- [HmacValidator API](../api-reference/hmac-validator.md)
- [Token Generation Behaviour](./token-generation.md)
- [Security Model](../security.md)