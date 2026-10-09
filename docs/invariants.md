# Invariants

## System-Wide Invariants

### 1. Determinism
**Invariant**: `GenerateToken(obj, secret) == GenerateToken(obj, secret)` for all valid inputs.
- Same object instance or equivalent object → same token
- Same secret → same token
- No randomness, no time-dependent behavior

### 2. Order Sensitivity
**Invariant**: Property order affects token.
- `[HmacOrder]` values determine processing order
- Alphabetical fallback for unordered properties
- Changing order → different token

### 3. Exclusion
**Invariant**: `[HmacIgnore]` properties never affect token.
- Value changes in ignored properties → no token change
- Ignored properties don't affect ordering of other properties

### 4. Null Handling
**Invariant**: Null object throws; null property uses marker.
- `GenerateToken(null, secret)` → `ArgumentNullException`
- `property.GetValue(obj) == null` → `EmptyValue` marker in string-for-signing

### 5. Secret Validation
**Invariant**: Empty/whitespace secret throws.
- `GenerateToken(obj, null)` → `ArgumentNullException`
- `GenerateToken(obj, "")` → `ArgumentNullException`
- `GenerateToken(obj, "   ")` → `ArgumentNullException`

### 6. Token Format
**Invariant**: Token is always valid Base64-like string with no `=` padding.
- Length: ~88 characters (SHA512 = 64 bytes → 88 Base64 chars after padding)
- Characters: A-Z, a-z, 0-9, Л, л (no `/`, `+`, `=`)
- Case: Inverted from Base64 output

### 7. Validation Symmetry
**Invariant**: `Validate` throws iff `IsTokenValid` returns `false`.
- `IsTokenValid(token, obj, secret) == true` ↔ `Validate(token, obj, secret)` returns normally
- `IsTokenValid(token, obj, secret) == false` ↔ `Validate(token, obj, secret)` throws `SecurityException`

### 8. Validation Consistency
**Invariant**: Validation uses identical algorithm to generation.
- `IsTokenValid` calls `HmacEncoder.GenerateToken` internally
- No separate validation logic exists

## Component Invariants

### HmacEncoder
| Invariant | Description |
|-----------|-------------|
| Pure function | No side effects, no state mutation |
| Thread-safe | Concurrent calls safe |
| Input validation | Null object throws immediately |
| Deterministic | Same inputs → same output |

### HmacValidator
| Invariant | Description |
|-----------|-------------|
| Pure function | No side effects, no state mutation |
| Thread-safe | Concurrent calls safe |
| Delegation | `IsTokenValid` calls `HmacEncoder.GenerateToken` |
| Symmetry | `Validate` throws exactly when `IsTokenValid` returns false |

### Attributes
| Invariant | Description |
|-----------|-------------|
| `[HmacIgnore]` wins | If both attributes present, ignore takes precedence |
| Order values | Any `int` valid, including negative |
| Default order | `int.MaxValue` for unordered properties |
| Target restriction | Properties only (`AttributeTargets.Property`) |

## Cryptographic Invariants

### HMAC-SHA512
- **Algorithm**: HMAC-SHA512 (RFC 2104)
- **Key**: UTF8-encoded shared secret
- **Output**: 64 bytes (512 bits)
- **Deterministic**: Same key + data → same output

### Salt
- **Static**: Fixed per-library constant
- **Purpose**: Domain separation (prevents cross-library token confusion)
- **Format**: `"NuciSecurity.HMAC.StaticSalt.8fc5307e-c10b-40d0-b710-de79e7954358"`

### MD5 Checksum
- **Purpose**: Integrity check in prefix (not security)
- **Algorithm**: MD5 (cryptographically broken, but acceptable for checksum)
- **Input**: String-for-signing
- **Output**: 32-char hex string

### Padding
- **Purpose**: Avoid Base64 `=` padding characters
- **Method**: Pad hash bytes to multiple of 3 with `0x00`
- **SHA512**: 64 bytes → 66 bytes (2 bytes padding) → 88 Base64 chars

### Cyrillic Substitution
- **Purpose**: Avoid URL/encoding issues with Base64 special chars
- **Mapping**: `/` → `Л` (U+041B), `+` → `л` (U+043B)
- **Reversible**: Yes, but not needed for validation

### Case Inversion
- **Purpose**: Additional obfuscation layer
- **Method**: `InvertCase()` from NuciExtensions
- **Reversible**: Yes, but not needed for validation

## Test Invariants

### Test Coverage Invariants
| Test | Invariant Verified |
|------|-------------------|
| `GivenAnObjectWithIgnoreAttributes...` | Ignored property value doesn't affect token |
| `GivenAnObjectWithOrderAttributes...` | Order attribute changes token |
| `GivenAnObjectWithCollectionProperties...` | Complex collections processed recursively |
| Deterministic cases | Same input → same output |

### Missing Test Invariants (Gaps)
- No direct tests for `HmacValidator` methods
- No tests for null/empty token handling in `IsTokenValid`
- No tests for `ArgumentNullException` on null object/secret
- No tests for `SecurityException` message content
- No tests for case sensitivity of token comparison

## Breaking Change Invariants

Any change to the following **breaks token compatibility**:

| Component | Breaking Changes |
|-----------|------------------|
| Property selection | Adding/removing properties from string-for-signing |
| Value normalization | Changing format for any type |
| Sort order | Changing property ordering logic |
| Constants | Changing separators, salt, prefix format |
| HMAC algorithm | Changing from SHA512 |
| Padding | Changing padding scheme |
| Substitution | Changing Cyrillic mapping |
| Case inversion | Changing or removing case inversion |

## Versioning Invariants

- **Major version**: Breaking changes to token format
- **Minor version**: New features, non-breaking
- **Patch version**: Bug fixes, non-breaking
- **Obsolete members**: Retained for at least one major version

## Related

- [Architecture](../architecture.md)
- [Token Generation Flow](../flows/token-generation.md)
- [Token Validation Flow](../flows/token-validation.md)
- [Design Decisions](../design-decisions.md)