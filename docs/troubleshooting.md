# Troubleshooting

## Common Issues

### 1. Token Validation Always Fails

**Symptoms**: `IsTokenValid` returns `false` for valid tokens.

**Causes & Solutions**:

| Cause | Solution |
|-------|----------|
| Different secret keys | Ensure both parties use identical secret |
| Object properties differ | Verify all non-ignored properties match exactly |
| Property order changed | Use `[HmacOrder]` for deterministic ordering |
| Collection order differs | Sort collections before token generation |
| Culture-dependent formatting | Use invariant culture for numbers/dates |

**Debugging**:
```csharp
// Compare string-for-signing on both sides
string senderString = HmacEncoder.GetStringForSigning(request, secret);
string receiverString = HmacEncoder.GetStringForSigning(receivedRequest, secret);

Console.WriteLine($"Sender: {senderString}");
Console.WriteLine($"Receiver: {receiverString}");
```

### 2. ArgumentNullException on GenerateToken

**Error**: `ArgumentNullException: Value cannot be null. (Parameter 'obj')`

**Cause**: Passing null object.

**Solution**:
```csharp
// Check before calling
if (request == null)
    throw new ArgumentNullException(nameof(request));

string token = HmacEncoder.GenerateToken(request, secret);
```

### 3. ArgumentNullException on Secret

**Error**: `ArgumentNullException: Value cannot be null. (Parameter 'sharedSecretKey')`

**Cause**: Secret is null, empty, or whitespace.

**Solution**:
```csharp
// Validate at startup
string secret = config["Hmac:Secret"];
if (string.IsNullOrWhiteSpace(secret))
    throw new InvalidOperationException("HMAC secret not configured");
```

### 4. SecurityException on Validate

**Error**: `SecurityException: The HMAC token is not valid.`

**Cause**: Token validation failed (mismatch or invalid format).

**Solution**: Use `IsTokenValid` for boolean check instead:
```csharp
if (!HmacValidator.IsTokenValid(token, request, secret))
    return Unauthorized();
```

### 5. Different Tokens for Same Object

**Symptoms**: Calling `GenerateToken` twice with same object/secret produces different tokens.

**Causes & Solutions**:

| Cause | Solution |
|-------|----------|
| Object mutated between calls | Ensure object is immutable during token generation |
| Collection enumeration order | Sort collections: `items.OrderBy(x => x)` |
| DateTime.Now in object | Use fixed timestamp or exclude with `[HmacIgnore]` |
| Random values in object | Exclude with `[HmacIgnore]` |

### 6. Nullable Warning CS8632

**Warning**: `CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.`

**Cause**: Project has `<Nullable>enable</Nullable>` but code lacks annotations.

**Solutions**:
```csharp
// Option 1: Add annotations
public string? OptionalProperty { get; set; }

// Option 2: Disable for file
#nullable disable
public class LegacyClass { ... }
#nullable restore

// Option 3: Disable for project (not recommended)
<Nullable>disable</Nullable>
```

### 7. NuciExtensions Not Found

**Error**: `CS0246: The type or namespace name 'NuciExtensions' could not be found`

**Cause**: Package not restored.

**Solution**:
```bash
dotnet restore
dotnet build
```

### 8. Tests Not Discovered

**Symptoms**: `dotnet test` shows 0 tests.

**Causes & Solutions**:

| Cause | Solution |
|-------|----------|
| Missing NUnit3TestAdapter | Add `<PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />` |
| Wrong test framework | Ensure `[Test]` attribute from NUnit |
| Build failed | Run `dotnet build` first |

## Debugging Token Generation

### Enable Detailed Logging

```csharp
public string GenerateTokenWithDebug<T>(T obj, string secret)
{
    var stringForSigning = HmacEncoder.GetStringForSigning(obj, secret);
    Console.WriteLine($"String for signing: {stringForSigning}");

    var token = HmacEncoder.GenerateToken(obj, secret);
    Console.WriteLine($"Generated token: {token}");

    return token;
}
```

### Inspect Property Order

```csharp
public void DebugPropertyOrder<T>(T obj)
{
    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
        .Select(p => new
        {
            Name = p.Name,
            Order = p.GetCustomAttribute<HmacOrderAttribute>()?.Order ?? int.MaxValue,
            Ignored = p.GetCustomAttribute<HmacIgnoreAttribute>() != null
        })
        .OrderBy(p => p.Order)
        .ThenBy(p => p.Name);

    foreach (var prop in properties)
    {
        Console.WriteLine($"Order={prop.Order} Ignored={prop.Ignored} {prop.Name}");
    }
}
```

## Environment-Specific Issues

### Linux/macOS Line Endings

**Issue**: Tokens differ between Windows and Linux.

**Cause**: `Environment.NewLine` differs (`\r\n` vs `\n`).

**Solution**: Normalize line endings in string-for-signing:
```csharp
// In your object, ensure consistent line endings
public string MultiLineProperty
{
    get => _value;
    set => _value = value?.Replace("\r\n", "\n");
}
```

### Culture-Specific Formatting

**Issue**: Numbers/dates format differently across cultures.

**Solution**: Use invariant culture:
```csharp
public class PaymentRequest
{
    public decimal Amount { get; set; }

    // Override ToString for invariant formatting
    public override string ToString() =>
        $"{MerchantId}|{OrderId}|{Amount.ToString(CultureInfo.InvariantCulture)}|{Currency}";
}
```

## Performance Issues

### Slow Token Generation

**Symptoms**: High latency on `GenerateToken`.

**Causes & Solutions**:

| Cause | Solution |
|-------|----------|
| Large object graphs | Exclude unnecessary properties with `[HmacIgnore]` |
| Reflection overhead | Cache property info (library does this per-type) |
| Large collections | Limit collection size or exclude |

### Memory Allocation

**Symptoms**: High GC pressure.

**Solutions**:
- Reuse objects where possible
- Avoid creating new objects per request
- Use structs for small payloads

## Getting Help

### Check These First

1. [ ] Same secret on both sides?
2. [ ] Same object properties (excluding ignored)?
3. [ ] Same property order (use `[HmacOrder]`)?
4. [ ] Collections sorted?
5. [ ] Culture-invariant formatting?
6. [ ] No null/empty secret?
7. [ ] No null object?

### Enable Diagnostics

```csharp
// Temporary diagnostic code
var props = typeof(MyDto).GetProperties()
    .Where(p => p.GetCustomAttribute<HmacIgnoreAttribute>() == null)
    .OrderBy(p => p.GetCustomAttribute<HmacOrderAttribute>()?.Order ?? int.MaxValue)
    .ThenBy(p => p.Name);

foreach (var prop in props)
{
    var value = prop.GetValue(myObject);
    Console.WriteLine($"{prop.Name}={value}");
}
```

### Report Issues

If issue persists, report with:
1. Minimal reproduction code
2. Expected vs actual token
3. String-for-signing from both sides
4. .NET version
5. Package version

## Related

- [Error Handling](../error-handling.md)
- [Security Model](../security.md)
- [Configuration](../configuration.md)