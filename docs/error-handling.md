# Error Handling

## Exception Taxonomy

| Exception | Source | Condition | Recovery |
|-----------|--------|-----------|----------|
| `ArgumentNullException` | `HmacEncoder.GenerateToken` | `obj` is null | Fix caller |
| `ArgumentNullException` | `HmacEncoder.GenerateToken` | `sharedSecretKey` null/empty/whitespace | Provide valid secret |
| `ArgumentNullException` | `HmacValidator.IsTokenValid` | `obj` is null | Fix caller |
| `ArgumentNullException` | `HmacValidator.IsTokenValid` | `sharedSecretKey` null/empty/whitespace | Provide valid secret |
| `SecurityException` | `HmacValidator.Validate` | Token invalid | Reject request |

## Exception Details

### ArgumentNullException

**Thrown by**: `HmacEncoder.GenerateToken`, `HmacValidator.IsTokenValid`, `HmacValidator.Validate`

**Conditions**:
- `obj` parameter is null
- `sharedSecretKey` parameter is null, empty, or whitespace

**Properties**:
- `ParamName`: Name of invalid parameter ("obj" or "sharedSecretKey")
- `Message`: Standard .NET message

**Handling**:
```csharp
try
{
    string token = HmacEncoder.GenerateToken(payload, secret);
}
catch (ArgumentNullException ex) when (ex.ParamName == "obj")
{
    // Handle null payload
    logger.LogError("Payload cannot be null");
}
catch (ArgumentNullException ex) when (ex.ParamName == "sharedSecretKey")
{
    // Handle missing secret
    logger.LogError("HMAC secret not configured");
}
```

### SecurityException

**Thrown by**: `HmacValidator.Validate`

**Condition**: Token validation fails (mismatch or null/empty token)

**Message**: `"The HMAC token is not valid."`

**Type**: `System.Security.SecurityException`

**Handling**:
```csharp
try
{
    HmacValidator.Validate(token, payload, secret);
}
catch (SecurityException)
{
    // Authentication failed
    return Unauthorized();
}
```

## Error Handling Patterns

### Pattern 1: Boolean Check (Recommended)

```csharp
bool isValid = HmacValidator.IsTokenValid(token, payload, secret);
if (!isValid)
{
    // Handle invalid token
    return Unauthorized();
}
// Proceed with valid payload
```

### Pattern 2: Exception-Based (Fail-Fast)

```csharp
try
{
    HmacValidator.Validate(token, payload, secret);
}
catch (SecurityException)
{
    // Handle invalid token
    return Unauthorized();
}
// Proceed with valid payload
```

### Pattern 3: Comprehensive

```csharp
public ValidationResult ValidateRequest(string token, Payload payload)
{
    if (string.IsNullOrWhiteSpace(token))
        return ValidationResult.MissingToken();

    if (payload == null)
        return ValidationResult.NullPayload();

    try
    {
        if (!HmacValidator.IsTokenValid(token, payload, _secret))
            return ValidationResult.InvalidToken();
    }
    catch (ArgumentNullException ex)
    {
        return ValidationResult.ConfigurationError(ex.ParamName);
    }

    return ValidationResult.Success();
}
```

## Error Messages

### User-Facing Messages

| Scenario | Message |
|----------|---------|
| Missing token | "Authentication token required" |
| Invalid token | "Invalid authentication token" |
| Missing secret | "Service configuration error" |
| Null payload | "Request payload required" |

### Internal Logging

```csharp
// Good: Log without sensitive data
logger.LogWarning("HMAC validation failed for merchant {MerchantId}", payload.MerchantId);

// Bad: Never log tokens or secrets
logger.LogError("Token: {Token}, Secret: {Secret}", token, secret);  // NEVER
```

## Recovery Strategies

### Client-Side (Token Generation)

| Error | Recovery |
|-------|----------|
| Null object | Ensure object instantiated before calling |
| Null secret | Load secret from configuration at startup |
| Empty secret | Validate configuration at startup |

### Server-Side (Token Validation)

| Error | Recovery |
|-------|----------|
| Invalid token | Return 401 Unauthorized |
| Missing token | Return 401 Unauthorized |
| Null payload | Return 400 Bad Request |
| Configuration error | Return 500 Internal Server Error (log alert) |

## Testing Error Paths

```csharp
[Test]
public void GenerateToken_NullObject_ThrowsArgumentNullException()
{
    Assert.Throws<ArgumentNullException>(() =>
        HmacEncoder.GenerateToken((TestDto)null, "secret"));
}

[Test]
public void GenerateToken_NullSecret_ThrowsArgumentNullException()
{
    var obj = new TestDto();
    Assert.Throws<ArgumentNullException>(() =>
        HmacEncoder.GenerateToken(obj, null));
}

[Test]
public void Validate_InvalidToken_ThrowsSecurityException()
{
    var obj = new TestDto { Value = "test" };
    string invalidToken = "invalid-token";

    Assert.Throws<SecurityException>(() =>
        HmacValidator.Validate(invalidToken, obj, "secret"));
}
```

## Related

- [Token Generation Behaviour](../behaviour/token-generation.md)
- [Token Validation Behaviour](../behaviour/token-validation.md)
- [Security Model](../security.md)