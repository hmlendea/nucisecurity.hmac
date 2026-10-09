# Ambiguities and Open Questions

## Known Ambiguities

### 1. Collection Ordering Guarantees

**Question**: What is the exact enumeration order for `IEnumerable<T>` properties?

**Current Behavior**: Uses `foreach` enumeration order.

**Ambiguity**:
- `List<T>` → insertion order (stable)
- `HashSet<T>` → undefined order (unstable)
- `Dictionary<TKey, TValue>` → undefined order (unstable)
- LINQ queries → depends on query

**Recommendation**: Document that only ordered collections (`List<T>`, `T[]`, `IReadOnlyList<T>`) produce deterministic tokens. Add validation or auto-sorting?

### 2. Null Property Handling

**Question**: How are null property values represented in the string-for-signing?

**Current Behavior**: `propertyName=` (empty value)

**Ambiguity**:
- Is `null` distinguishable from empty string `""`?
- Current: Both produce `propertyName=`

**Impact**: Object with `Name = null` produces same token as `Name = ""`.

### 3. DateTime Serialization

**Question**: What DateTime format is used?

**Current Behavior**: `DateTime.ToString()` → culture-dependent.

**Ambiguity**:
- `en-US`: `1/15/2026 2:30:00 PM`
- `de-DE`: `15.01.2026 14:30:00`
- `InvariantCulture`: `01/15/2026 14:30:00`

**Recommendation**: Use `ToString("o")` (ISO 8601) or `ToString(CultureInfo.InvariantCulture)`.

### 4. Decimal/Float Precision

**Question**: What precision for numeric types?

**Current Behavior**: `ToString()` → culture-dependent, variable precision.

**Ambiguity**:
- `1.0` vs `1.00` vs `1`
- Scientific notation for large values

**Recommendation**: Use `ToString("G", CultureInfo.InvariantCulture)` or fixed format.

### 5. Enum Representation

**Question**: How are enum values represented?

**Current Behavior**: `Enum.ToString()` → name by default.

**Ambiguity**:
- `[Flags]` enums → comma-separated names
- Numeric value vs name
- Case sensitivity

### 6. Nested Object Handling

**Question**: Are nested objects supported?

**Current Behavior**: `object.ToString()` → typically `Namespace.TypeName`.

**Ambiguity**:
- No recursive property enumeration
- Complex objects not meaningfully included

**Recommendation**: Document as unsupported or implement recursive serialization.

### 7. Interface/Abstract Properties

**Question**: Are interface properties included?

**Current Behavior**: `GetProperties()` on concrete type includes all public instance properties.

**Ambiguity**:
- Explicit interface implementations excluded
- Properties from base classes included

### 8. Static Salt Purpose

**Question**: Why is the salt a GUID-like string?

**Current Value**: `"NuciSecurity.HMAC.StaticSalt.8fc5307e-c10b-40d0-b710-de79e7954358"`

**Ambiguity**:
- Is the GUID portion meaningful?
- Could it be any string?
- Does it need to be secret?

**Answer**: Domain separation only. Not secret. Any unique string works.

## Open Questions

### 1. Algorithm Agility

**Question**: Should the library support multiple HMAC algorithms?

**Current**: Hardcoded HMAC-SHA512.

**Options**:
- Keep single algorithm (simpler, more secure)
- Add `HmacAlgorithm` parameter (flexible, complex)
- Support via configuration (breaking change)

**Decision Needed**: Before v5.0.

### 2. Token Format Versioning

**Question**: How to version token format for future changes?

**Current**: No version in token.

**Options**:
- Prefix with version byte
- Include in string-for-signing
- Separate validation methods per version

**Decision Needed**: Before any breaking format change.

### 3. Constant-Time Comparison

**Question**: Should validation use constant-time comparison?

**Current**: `String.Equals` (ordinal, not constant-time).

**Analysis**:
- Attacker knows the token (it's transmitted)
- Timing attack reveals nothing new
- Constant-time adds complexity

**Decision**: Current approach acceptable. Document rationale.

### 4. Async API

**Question**: Should async methods be added?

**Current**: Synchronous only.

**Options**:
- Add `GenerateTokenAsync` / `ValidateAsync`
- Keep sync (HMAC is fast, CPU-bound)
- Let callers wrap in `Task.Run`

**Decision**: Keep sync. HMAC-SHA512 is fast (< 1ms typical).

### 5. Streaming Large Objects

**Question**: Support for streaming large object graphs?

**Current**: All properties loaded into memory for string-for-signing.

**Options**:
- Keep as-is (typical use case: small DTOs)
- Add `IHmacSerializable` interface
- Stream properties to HMAC incrementally

**Decision**: Keep as-is. Document size limits.

### 6. Key Derivation

**Question**: Should the library derive keys from a master secret?

**Current**: Direct secret usage.

**Options**:
- Add HKDF-based key derivation
- Support key IDs for rotation
- Keep simple (caller manages keys)

**Decision**: Keep simple. Key management is caller responsibility.

### 7. Replay Protection Built-In

**Question**: Should the library include timestamp/nonce validation?

**Current**: No. Caller must include in object.

**Options**:
- Add `Timestamp` property auto-validation
- Add `Nonce` tracking (requires state)
- Keep stateless (current)

**Decision**: Keep stateless. Replay protection is application concern.

### 8. .NET Standard / Multi-Targeting

**Question**: Should the library target .NET Standard 2.0?

**Current**: net10.0 only.

**Options**:
- Multi-target: netstandard2.0, net8.0, net10.0
- Keep net10.0 only (modern APIs)
- Add net8.0 (LTS)

**Decision**: Consider net8.0 for broader compatibility.

## Resolved Questions

### ✅ Why NuciExtensions?

**Answer**: Provides `Reverse()` and `InvertCase()` for token obfuscation. Author is same (Horațiu Mlendea). GPL-compatible.

### ✅ Why MD5 in prefix?

**Answer**: Checksum only, not security. Detects corruption. MD5 acceptable for non-cryptographic checksum.

### ✅ Why obfuscation layers?

**Answer**: Defense in depth. Makes tokens less recognizable as HMAC. Not a security control.

### ✅ Why GPL-3.0?

**Answer**: Author's choice. NuciExtensions is GPL-3.0. Compatible with GPL ecosystem.

## Tracking

| ID | Question | Status | Priority |
|----|----------|--------|----------|
| AMB-001 | Collection ordering | Open | High |
| AMB-002 | Null vs empty string | Open | Medium |
| AMB-003 | DateTime format | Open | High |
| AMB-004 | Numeric precision | Open | Medium |
| AMB-005 | Enum representation | Open | Low |
| AMB-006 | Nested objects | Open | Medium |
| AMB-007 | Interface properties | Open | Low |
| AMB-008 | Static salt purpose | Resolved | - |
| OQ-001 | Algorithm agility | Open | Medium |
| OQ-002 | Token versioning | Open | High |
| OQ-003 | Constant-time compare | Resolved | - |
| OQ-004 | Async API | Resolved | - |
| OQ-005 | Streaming | Open | Low |
| OQ-006 | Key derivation | Open | Low |
| OQ-007 | Replay protection | Resolved | - |
| OQ-008 | Multi-targeting | Open | Medium |

## Related

- [Design Decisions](../design-decisions.md)
- [Security Model](../security.md)
- [Change Guide](../change-guide.md)