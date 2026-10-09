# Token Validation Flow

**Entry Points**:
- `HmacValidator.IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey)`
- `HmacValidator.Validate<TObject>(string expectedToken, TObject obj, string sharedSecretKey)`

## Complete Execution Trace

```
IsTokenValid(expectedToken, obj, secret)
│
├─► [1] Token Null/Empty Check
│   └─► if (string.IsNullOrWhiteSpace(expectedToken)) return false
│
├─► [2] Input Validation
│   ├─► ArgumentNullException.ThrowIfNull(obj)
│   └─► ArgumentNullException.ThrowIfNullOrWhiteSpace(sharedSecretKey)
│
├─► [3] Token Generation
│   └─► string generatedToken = HmacEncoder.GenerateToken(obj, secret)
│       │
│       │ (See: flows/token-generation.md for full trace)
│       │
│       └─► [3.1] GetStringForSigning(obj)
│       └─► [3.2] Build Prefix
│       └─► [3.3] ComputeHmacToken(prefix + reversed, secret)
│       └─► [3.4] InvertCase(result)
│
├─► [4] Comparison
│   └─► return generatedToken.Equals(expectedToken)
│
└─► [5] Return Result
    ├─► true  → Token valid
    └─► false → Token invalid

Validate(expectedToken, obj, secret)
│
├─► [1] Delegate to IsTokenValid
│   └─► bool isValid = IsTokenValid(expectedToken, obj, secret)
│
├─► [2] Check Result
│   ├─► if (isValid) return  // No exception
│   └─► if (!isValid) throw new SecurityException("The HMAC token is not valid.")
│
└─► [3] Return or Throw
```

## Detailed Step Analysis

### IsTokenValid

#### Step 1: Token Null/Empty Check
```csharp
if (string.IsNullOrWhiteSpace(expectedToken))
{
    return false;
}
```
- **Purpose**: Early exit for empty tokens
- **Behavior**: Returns `false` (not exception)
- **Rationale**: Empty token is invalid, not an error condition

#### Step 2: Input Validation
```csharp
ArgumentNullException.ThrowIfNull(obj);
ArgumentNullException.ThrowIfNullOrWhiteSpace(sharedSecretKey);
```
- **Purpose**: Fail fast on invalid inputs
- **Exceptions**:
  - `ArgumentNullException` if `obj` is null
  - `ArgumentNullException` if `sharedSecretKey` is null/empty/whitespace

#### Step 3: Token Generation
```csharp
string generatedToken = HmacEncoder.GenerateToken(obj, sharedSecretKey);
```
- **Delegates** to `HmacEncoder.GenerateToken`
- **Full trace**: See [Token Generation Flow](./token-generation.md)
- **Key insight**: Validation is **regeneration + comparison**
- **No separate validation logic**: Token is valid iff it matches regenerated token

#### Step 4: Comparison
```csharp
return generatedToken.Equals(expectedToken);
```
- **Method**: `string.Equals` (ordinal, case-sensitive)
- **Not constant-time**: Acceptable for HMAC validation
- **Exact match**: No trimming, no normalization
- **Case-sensitive**: Token case matters (due to `InvertCase`)

#### Step 5: Return
- `true` → Token matches
- `false` → Token doesn't match

### Validate

#### Step 1: Delegate
```csharp
if (!IsTokenValid(expectedToken, obj, sharedSecretKey))
```
- Calls `IsTokenValid` with same parameters
- All validation logic in `IsTokenValid`

#### Step 2: Check Result
```csharp
{
    throw new SecurityException("The HMAC token is not valid.");
}
```
- **Exception**: `System.Security.SecurityException`
- **Message**: `"The HMAC token is not valid."`
- **Type**: Security exception (not generic exception)

#### Step 3: Return or Throw
- `true` → No exception, method returns
- `false` → `SecurityException` thrown

## Validation Semantics

### Comparison Method

```csharp
generatedToken.Equals(expectedToken)
```

- **Ordinal comparison**: Byte-by-byte, case-sensitive
- **No culture awareness**: Invariant culture
- **No trimming**: Whitespace matters
- **No normalization**: Exact string match required

### Why Regeneration?

Validation works by **regenerating** the token and comparing:

1. **Simplicity**: No separate validation logic to maintain
2. **Consistency**: Guaranteed to match generation algorithm
3. **Determinism**: Same inputs always produce same token
4. **Security**: HMAC properties ensure only valid tokens match

### Timing Considerations

- **Not constant-time**: `string.Equals` short-circuits on first difference
- **Acceptable**: Attacker cannot observe timing in typical HMAC use cases
- **Mitigation**: Token is already known to attacker (they generated it)

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
| Secret whitespace | `ArgumentNullException` | `SecurityException` |

## SecurityException Details

- **Type**: `System.Security.SecurityException`
- **Message**: `"The HMAC token is not valid."`
- **Source**: `HmacValidator.Validate`
- **Purpose**: Signal authentication failure to caller
- **Handling**: Caller should treat as unauthorized request

## Dependencies

### Internal
- `HmacEncoder.GenerateToken` — Token regeneration for comparison

### External
- `System.Security.SecurityException` — Exception type
- `System.ArgumentNullException` — Input validation
- `string.Equals` — Comparison (System.String)

## Thread Safety

All methods are **thread-safe** — no shared mutable state.

## Test Coverage

No direct tests for `HmacValidator` — tested indirectly:

| Test | Validates |
|------|-----------|
| `GivenAnObjectWithIgnoreAttributes...` | Ignored properties don't affect validation |
| `GivenAnObjectWithOrderAttributes...` | Order affects validation |
| `GivenAnObjectWithCollectionProperties...` | Collections validated correctly |

**Gap**: No direct tests for:
- `IsTokenValid` returning `false` for invalid tokens
- `Validate` throwing `SecurityException`
- Null/empty token handling in `IsTokenValid`
- Null object/secret throwing `ArgumentNullException`

## Invariants

1. **Symmetry**: `Validate` throws iff `IsTokenValid` returns `false`
2. **Determinism**: Same inputs → same validation result
3. **Consistency**: Validation uses same algorithm as generation
4. **Fail-closed**: Invalid token → `false` / exception (never `true`)

## Modification Impact

| Change | Risk |
|--------|------|
| Comparison method | **High** — affects validation strictness |
| Exception type | **High** — breaks consumer error handling |
| Exception message | **Medium** — may break message-based handling |
| Null token handling | **Medium** — changes API contract |
| Input validation | **Medium** — changes error semantics |

**Any change to validation logic is a breaking change for consumers.**