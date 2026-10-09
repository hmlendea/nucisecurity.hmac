# Token Generation Flow

**Entry Point**: `HmacEncoder.GenerateToken<TObject>(TObject obj, string sharedSecretKey)`

## Complete Execution Trace

```
GenerateToken(obj, secret)
│
├─► [1] Input Validation
│   ├─► ArgumentNullException.ThrowIfNull(obj)
│   └─► (secret validated later in ComputeHmacToken)
│
├─► [2] GetStringForSigning(obj)
│   │
│   ├─► [2.1] Null Check
│   │   └─► if (obj is null) return EmptyValue + FieldSeparator
│   │
│   ├─► [2.2] Property Enumeration
│   │   └─► obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
│   │
│   ├─► [2.3] Filter Ignored Properties
│   │   └─► .Where(p => p.GetCustomAttribute<HmacIgnoreAttribute>() is null)
│   │
│   ├─► [2.4] Extract Order Attributes
│   │   └─► .Select(p => new { Property = p, OrderAttr = p.GetCustomAttribute<HmacOrderAttribute>() })
│   │
│   ├─► [2.5] Sort Properties
│   │   ├─► .OrderBy(x => x.OrderAttr?.Order ?? DefaultOrder)  // DefaultOrder = int.MaxValue
│   │   └─► .ThenBy(x => x.Property.Name)
│   │
│   ├─► [2.6] Select Properties
│   │   └─► .Select(x => x.Property)
│   │
│   ├─► [2.7] For Each Property (StringBuilder)
│   │   │
│   │   ├─► [2.7.1] Get Value
│   │   │   └─► property.GetValue(obj)
│   │   │
│   │   ├─► [2.7.2] Check Collection
│   │   │   └─► if (propertyValue is IEnumerable enumerable && propertyValue is not string)
│   │   │
│   │   ├─► [2.7.3a] Complex Collection (element is class, not string)
│   │   │   ├─► Get element type
│   │   │   ├─► if (elementType.IsClass && elementType != typeof(string))
│   │   │   ├─► StringBuilder nestedBuilder = new()
│   │   │   ├─► foreach (var item in enumerable)
│   │   │   │   └─► nestedBuilder.Append(item is null ? EmptyValue : GetStringForSigning(item) + FieldSeparator)
│   │   │   └─► value = nestedBuilder.ToString()
│   │   │
│   │   ├─► [2.7.3b] Scalar Collection
│   │   │   ├─► var flatValues = enumerable.Cast<object?>().Select(item => item?.ToString() ?? EmptyValue)
│   │   │   └─► value = string.Join(FieldSeparator, flatValues)
│   │   │
│   │   ├─► [2.7.4] Single Value Normalization
│   │   │   └─► propertyValue switch
│   │   │       ├─► DateTime dt => dt.ToString("O")
│   │   │       ├─► bool b => b.ToString().ToLowerInvariant()
│   │   │       └─► _ => propertyValue?.ToString() ?? EmptyValue
│   │   │
│   │   └─► [2.7.5] Append to Main Builder
│   │       └─► stringBuilder.Append(value + FieldSeparator)
│   │
│   └─► [2.8] Return String
│       └─► return stringBuilder.ToString()
│
├─► [3] Build Prefix
│   ├─► length = stringForSigning.Length
│   ├─► checksum = GetMd5Hash(stringForSigning)
│   └─► prefix = string.Format(PrefixFormat, length, checksum)
│       // PrefixFormat = "|#Length:{0};Checksum:{1}#|"
│
├─► [4] ComputeHmacToken(prefix + stringForSigning.Reverse(), secret)
│   │
│   ├─► [4.1] Input Validation
│   │   ├─► ArgumentNullException.ThrowIfNullOrWhiteSpace(stringForSigning)
│   │   └─► ArgumentNullException.ThrowIfNullOrWhiteSpace(sharedSecretKey)
│   │
│   ├─► [4.2] Create HMAC
│   │   └─► using HMACSHA512 hmac = new(Encoding.UTF8.GetBytes(sharedSecretKey))
│   │
│   ├─► [4.3] Salt Input
│   │   └─► string saltedString = $"{StaticSalt}.{stringForSigning}"
│   │       // StaticSalt = "NuciSecurity.HMAC.StaticSalt.8fc5307e-c10b-40d0-b710-de79e7954358"
│   │
│   ├─► [4.4] Compute Hash
│   │   ├─► byte[] bytesToSign = Encoding.UTF8.GetBytes(saltedString)
│   │   └─► byte[] keyBytes = hmac.ComputeHash(bytesToSign)
│   │
│   ├─► [4.5] Pad Bytes
│   │   └─► keyBytes = PadBytesToAvoidBase64Equals(keyBytes)
│   │       // Pads to multiple of 3 to avoid Base64 '=' padding
│   │
│   ├─► [4.6] Base64 Encode
│   │   └─► string base64 = Convert.ToBase64String(keyBytes)
│   │
│   ├─► [4.7] Cyrillic Substitution
│   │   ├─► base64.Replace("/", "Л")   // U+041B CYRILLIC CAPITAL LETTER EL
│   │   └─► base64.Replace("+", "л")   // U+043B CYRILLIC SMALL LETTER EL
│   │
│   └─► [4.8] Return HMAC String
│       └─► return substitutedBase64
│
├─► [5] Case Inversion
│   └─► return hmacToken.InvertCase()  // From NuciExtensions
│
└─► [6] Return Final Token
```

## Detailed Step Analysis

### Step 1: Input Validation
- **Purpose**: Fail fast on null object
- **Exception**: `ArgumentNullException` if `obj` is null
- **Secret**: Validated later in `ComputeHmacToken`

### Step 2: GetStringForSigning
**Core algorithm** — builds deterministic string representation of object.

#### 2.1 Null Object Handling
- Returns `EmptyValue + FieldSeparator` (`"|#EmptyValue#||#FieldSeparator#|"`)
- Ensures null objects produce consistent (empty) tokens

#### 2.2-2.6 Property Selection & Sorting
- **Reflection**: `GetProperties(BindingFlags.Public | BindingFlags.Instance)`
- **Filter**: Exclude `[HmacIgnore]` attributes
- **Sort Key**: `(OrderAttr?.Order ?? int.MaxValue, PropertyName)`
- **Result**: Ordered `IEnumerable<PropertyInfo>`

#### 2.7 Per-Property Processing
**Collection Detection**:
```csharp
propertyValue is IEnumerable enumerable && propertyValue is not string
```

**Complex Collection** (element type is class ≠ string):
- Recursive `GetStringForSigning` per item
- Null items → `EmptyValue`
- Each item terminated with `FieldSeparator`

**Scalar Collection**:
- `Cast<object?>()`
- `Select(item => item?.ToString() ?? EmptyValue)`
- `Join(FieldSeparator, ...)`

**Single Value**:
- `DateTime` → `ToString("O")` (round-trip ISO 8601)
- `bool` → `"true"`/`"false"` (lowercase invariant)
- `null` → `EmptyValue`
- Other → `ToString()`

#### 2.8 Result
- All values joined with `FieldSeparator`
- Trailing `FieldSeparator` included

### Step 3: Prefix Construction
```
|#Length:{stringForSigning.Length};Checksum:{MD5(stringForSigning)}#|
```
- **Length**: Character count of string-for-signing
- **Checksum**: MD5 hex (32 chars) of string-for-signing
- **Purpose**: Quick integrity check, not security

### Step 4: ComputeHmacToken
**Input**: `prefix + stringForSigning.Reverse()`

#### 4.1 Validation
- Both inputs must be non-null, non-empty, non-whitespace

#### 4.2 HMAC Creation
- `HMACSHA512` with UTF8-encoded secret key
- `Initialize()` called explicitly

#### 4.3 Salting
- `StaticSalt + "." + inputString`
- Prevents cross-library token confusion

#### 4.4 Hash Computation
- UTF8 encode salted string
- HMAC-SHA512 → 64 bytes

#### 4.5 Padding
- SHA512 = 64 bytes, 64 % 3 = 1
- Pad with 2 bytes of `0x00` → 66 bytes
- 66 bytes → 88 Base64 chars (no `=` padding)

#### 4.6 Base64 Encoding
- Standard Base64

#### 4.7 Cyrillic Substitution
- `/` → `Л` (U+041B)
- `+` → `л` (U+043B)
- Avoids URL/encoding issues with Base64 special chars

### Step 5: Case Inversion
- `InvertCase()` from `NuciExtensions.StringCasingExtensions`
- Flips case of each character
- Additional obfuscation layer

### Step 6: Return
- Final token string returned to caller

## Data Transformations Summary

| Stage | Input | Transformation | Output |
|-------|-------|----------------|--------|
| 1 | Object | Validation | Validated object |
| 2 | Object | Reflection + normalization | String-for-signing |
| 3 | String-for-signing | Prefix format | Prefix string |
| 4 | Prefix + Reversed string | HMAC-SHA512 + encoding | Base64-like string |
| 5 | Base64-like string | Case inversion | Final token |

## Constants Used

| Constant | Value |
|----------|-------|
| `StaticSalt` | `"NuciSecurity.HMAC.StaticSalt.8fc5307e-c10b-40d0-b710-de79e7954358"` |
| `FieldSeparator` | `"|#FieldSeparator#|"` |
| `EmptyValue` | `"|#EmptyValue#|"` |
| `PrefixFormat` | `"|#Length:{0};Checksum:{1}#|"` |
| `DefaultOrder` | `int.MaxValue` |

## Error Paths

| Location | Condition | Exception |
|----------|-----------|-----------|
| GenerateToken | `obj == null` | `ArgumentNullException` |
| ComputeHmacToken | `stringForSigning` null/empty/whitespace | `ArgumentNullException` |
| ComputeHmacToken | `sharedSecretKey` null/empty/whitespace | `ArgumentNullException` |

## Invariants Maintained

1. **Deterministic**: Same inputs → same output (always)
2. **Order-sensitive**: Property order affects output
3. **Exclusion**: `[HmacIgnore]` properties never in string-for-signing
4. **Null-safe**: Null properties → `EmptyValue` marker
5. **Format-stable**: Token always valid Base64-like, no `=` padding

## Test Verification Points

| Test | Verifies |
|------|----------|
| `GivenAnObjectWithIgnoreAttributes...` | Ignored properties don't affect token |
| `GivenAnObjectWithOrderAttributes...` | Order attribute changes token |
| `GivenAnObjectWithCollectionProperties...` | Complex collections processed recursively |
| Deterministic cases | Same input → same output |

## Modification Impact

| Change | Risk |
|--------|------|
| Property selection logic | **High** — breaks all existing tokens |
| Value normalization | **High** — breaks all existing tokens |
| Sort order | **High** — breaks tokens for affected types |
| Constants (separators, salt) | **High** — breaks all existing tokens |
| HMAC algorithm | **High** — breaks all existing tokens |
| Padding/substitution | **High** — breaks all existing tokens |
| Case inversion | **High** — breaks all existing tokens |

**Any change to this flow is a breaking change for token compatibility.**