# HmacEncoder Component

**File**: `NuciSecurity.HMAC/HmacEncoder.cs`
**Type**: `static class`
**Purpose**: Core token generation logic

## Public API

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GenerateToken` | `GenerateToken<TObject>(TObject obj, string sharedSecretKey)` | Main entry point |
| `IsTokenValid` | `IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey)` | [Obsolete] Delegates to HmacValidator |

## Internal Methods

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetStringForSigning` | `GetStringForSigning<TObject>(TObject obj)` | Builds string-for-signing from object |
| `ComputeHmacToken` | `ComputeHmacToken(string stringForSigning, string sharedSecretKey)` | Computes HMAC from string |
| `PadBytesToAvoidBase64Equals` | `PadBytesToAvoidBase64Equals(byte[] input, byte padByte = 0x00)` | Pads hash to avoid Base64 `=` |
| `GetMd5Hash` | `GetMd5Hash(string input)` | Computes MD5 checksum |

## Constants

| Constant | Value | Purpose |
|----------|-------|---------|
| `StaticSalt` | `"NuciSecurity.HMAC.StaticSalt.8fc5307e-c10b-40d0-b710-de79e7954358"` | Library-specific salt |
| `FieldSeparator` | `"|#FieldSeparator#|"` | Separates property values |
| `EmptyValue` | `"|#EmptyValue#|"` | Represents null values |
| `PrefixFormat` | `"|#Length:{0};Checksum:{1}#|"` | Prefix format with length + MD5 |
| `DefaultOrder` | `int.MaxValue` | Default sort order for properties without `[HmacOrder]` |

## GetStringForSigning Algorithm

```csharp
static string GetStringForSigning<TObject>(TObject obj) where TObject : class
{
    if (obj is null) return EmptyValue + FieldSeparator;

    var propertiesToCompute = obj.GetType()
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<HmacIgnoreAttribute>() is null)
        .Select(p => new { Property = p, OrderAttr = p.GetCustomAttribute<HmacOrderAttribute>() })
        .OrderBy(x => x.OrderAttr?.Order ?? DefaultOrder)
        .ThenBy(x => x.Property.Name)
        .Select(x => x.Property);

    foreach (PropertyInfo property in propertiesToCompute)
    {
        var propertyValue = property.GetValue(obj);
        string value;

        if (propertyValue is IEnumerable enumerable && propertyValue is not string)
        {
            Type elementType = property.PropertyType.IsGenericType
                ? property.PropertyType.GetGenericArguments()[0]
                : property.PropertyType.GetElementType();

            if (elementType is not null && elementType.IsClass && elementType != typeof(string))
            {
                // Complex object collection - recurse
                StringBuilder nestedBuilder = new();
                foreach (var item in enumerable)
                {
                    nestedBuilder.Append(item is null ? EmptyValue : GetStringForSigning(item) + FieldSeparator);
                }
                value = nestedBuilder.ToString();
            }
            else
            {
                // Scalar collection - flatten
                var flatValues = enumerable.Cast<object?>().Select(item => item?.ToString() ?? EmptyValue);
                value = string.Join(FieldSeparator, flatValues);
            }
        }
        else
        {
            // Single value - normalize
            value = propertyValue switch
            {
                DateTime dt => dt.ToString("O"),
                bool b => b.ToString().ToLowerInvariant(),
                _ => propertyValue?.ToString() ?? EmptyValue
            };
        }

        stringBuilder.Append(value + FieldSeparator);
    }

    return stringBuilder.ToString();
}
```

### Property Selection Rules

1. **Public instance properties only** — `BindingFlags.Public | BindingFlags.Instance`
2. **Exclude `[HmacIgnore]`** — Filtered out before sorting
3. **Sort order**:
   - First: Properties with `[HmacOrder]` by `Order` ascending
   - Then: Remaining properties by name alphabetically
   - Default order: `int.MaxValue` (processed last)

### Value Normalization

| Input Type | Output Format |
|------------|---------------|
| `DateTime` | `ToString("O")` — ISO 8601 round-trip (e.g., `2026-10-09T12:30:45.1234567Z`) |
| `bool` | `"true"` / `"false"` (lowercase invariant) |
| `null` | `EmptyValue` marker (`|#EmptyValue#|`) |
| Complex object (in collection) | Recursive `GetStringForSigning` |
| Scalar collection | `Join(FieldSeparator, items.Select(ToString))` |
| Other | `ToString()` |

### Collection Handling

**Complex Object Collections** (element type is class, not string):
- Each item processed recursively via `GetStringForSigning`
- Null items become `EmptyValue`
- Results concatenated with `FieldSeparator`

**Scalar Collections** (primitives, strings, structs):
- Each item converted to string (null → `EmptyValue`)
- Joined with `FieldSeparator`

## ComputeHmacToken Algorithm

```csharp
static string ComputeHmacToken(string stringForSigning, string sharedSecretKey)
{
    ArgumentNullException.ThrowIfNullOrWhiteSpace(stringForSigning);
    ArgumentNullException.ThrowIfNullOrWhiteSpace(sharedSecretKey);

    using HMACSHA512 hmac = new(Encoding.UTF8.GetBytes(sharedSecretKey));
    hmac.Initialize();

    string saltedString = $"{StaticSalt}.{stringForSigning}";
    byte[] bytesToSign = Encoding.UTF8.GetBytes(saltedString);
    byte[] keyBytes = PadBytesToAvoidBase64Equals(hmac.ComputeHash(bytesToSign));

    return Convert
        .ToBase64String(keyBytes)
        .Replace("/", "Л")
        .Replace("+", "л");
}
```

### Steps

1. **Validate inputs** — Throw if null/empty/whitespace
2. **Create HMACSHA512** — Key = UTF8 bytes of secret
3. **Salt** — Prepend `StaticSalt + "."` to input string
4. **Hash** — Compute HMAC-SHA512
5. **Pad** — Pad hash bytes to multiple of 3 (avoids Base64 `=` padding)
6. **Base64** — Convert to Base64 string
7. **Substitute** — Replace `/`→`Л`, `+`→`л` (Cyrillic lookalikes)
8. **Return** — Final token (case inversion applied by caller)

## PadBytesToAvoidBase64Equals

```csharp
static byte[] PadBytesToAvoidBase64Equals(byte[] input, byte padByte = 0x00)
{
    int padLength = (3 - (input.Length % 3)) % 3;
    if (padLength == 0) return input;

    byte[] padded = new byte[input.Length + padLength];
    Buffer.BlockCopy(input, 0, padded, 0, input.Length);
    for (int i = 0; i < padLength; i++)
    {
        padded[input.Length + i] = padByte;
    }
    return padded;
}
```

**Purpose**: Base64 encodes 3 bytes → 4 chars. If input length not multiple of 3, Base64 adds `=` padding. This pads with `0x00` bytes to avoid `=` in output.

**SHA512 Output**: 64 bytes (512 bits). 64 % 3 = 1, so 2 bytes of padding added → 66 bytes → 88 Base64 chars (no `=`).

## GetMd5Hash

```csharp
static string GetMd5Hash(string input)
{
    byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
    StringBuilder sb = new();
    foreach (byte b in hashBytes) sb.Append(b.ToString("x2"));
    return sb.ToString();
}
```

**Purpose**: Checksum in prefix for integrity verification (not security).

## Dependencies

### External
- `System.Reflection` — Property enumeration, attributes
- `System.Security.Cryptography` — HMACSHA512, MD5
- `System.Text` — StringBuilder, Encoding
- `System.Collections` — IEnumerable handling
- `System.Linq` — Query operations
- `NuciExtensions` — `Reverse()`, `InvertCase()` extension methods

### Internal
- None (all methods static, no shared state)

## Thread Safety

All methods are **thread-safe** — no shared mutable state, pure functions of inputs.

## Performance Characteristics

| Operation | Complexity | Notes |
|-----------|------------|-------|
| Property enumeration | O(P) | P = public instance properties |
| Property sorting | O(P log P) | By order then name |
| String building | O(V) | V = total value string length |
| HMAC computation | O(1) | Fixed 64-byte output |
| Base64 encoding | O(1) | Fixed ~88 char output |

## Test Coverage

See `NuciSecurity.HMAC.UnitTests/HmacEncoderTests.cs`:
- `[HmacIgnore]` attribute behavior (2 test cases)
- `[HmacOrder]` attribute behavior (2 test cases)
- Collection property handling (1 test case)
- Deterministic generation (2 test cases with different ignored values)

## Modification Impact

| Change | Affected Areas |
|--------|----------------|
| Property selection logic | Token format, validation compatibility |
| Value normalization | Token format, validation compatibility |
| Sort order | Token format, validation compatibility |
| Constants (separators, salt) | Token format, validation compatibility |
| HMAC algorithm | Security, token format |
| Padding/substitution | Token format, validation compatibility |

**Breaking**: Any change to token generation algorithm breaks validation for existing tokens.