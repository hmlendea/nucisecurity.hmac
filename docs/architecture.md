# Architecture

This document complements the root [ARCHITECTURE.md](../ARCHITECTURE.md) with implementation-level detail.

## System Context

```
┌─────────────────────────────────────────────────────────────────┐
│                      Trusted Party A                            │
│  ┌──────────────┐    GenerateToken(payload, secret)             │
│  │  Application │ ──────────────────────────────────────────►   │
│  └──────────────┘          HMAC Token                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Transport Layer                            │
│  (HTTP header, query param, message body, etc.)                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Trusted Party B                            │
│  ┌──────────────┐    Validate(token, payload, secret)           │
│  │  Application │ ◄──────────────────────────────────────────   │
│  └──────────────┘          Valid / Invalid                      │
└─────────────────────────────────────────────────────────────────┘
```

## Module Structure

```
NuciSecurity.HMAC (net10.0)
├── HmacEncoder.cs          # Token generation (static)
├── HmacValidator.cs        # Token validation (static)
├── HmacIgnoreAttribute.cs  # Exclusion attribute
├── HmacOrderAttribute.cs   # Ordering attribute
└── NuciSecurity.HMAC.csproj

NuciSecurity.HMAC.UnitTests (net10.0)
├── HmacEncoderTests.cs     # Encoder tests
└── Helpers/                # Test object models
```

## Component Interaction Diagram

```
                    ┌─────────────────────┐
                    │   Client Code       │
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              ▼                ▼                ▼
       ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
       │ HmacEncoder │  │HmacValidator│  │  Attributes │
       │ GenerateToken│  │IsTokenValid │  │ [HmacIgnore]│
       │             │  │Validate     │  │ [HmacOrder] │
       └──────┬──────┘  └──────┬──────┘  └──────┬──────┘
              │                │                │
              └────────────────┼────────────────┘
                               │
              ┌────────────────┼────────────────┐
              ▼                ▼                ▼
       ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
       │Reflection   │  │Cryptography │  │NuciExtensions│
       │Property Enum│  │HMAC-SHA512  │  │Reverse()    │
       │Attribute Read│  │MD5          │  │InvertCase() │
       └─────────────┘  └─────────────┘  └─────────────┘
```

## Data Flow: Token Generation

```
Input Object
     │
     ▼
┌──────────────────────────────────────────────────────────────┐
│ GetStringForSigning                                          │
│ 1. Get public instance properties via Reflection             │
│ 2. Filter: exclude [HmacIgnore]                              │
│ 3. Sort: [HmacOrder] by Order asc, then by Name asc          │
│ 4. For each property:                                        │
│    ├─ Get value                                              │
│    ├─ If IEnumerable (not string):                           │
│    │   ├─ If element is class → recurse GetStringForSigning  │
│    │   └─ Else → Join(FieldSeparator, ToString())            │
│    └─ Else: Normalize value                                  │
│        ├─ DateTime → "O" format                              │
│        ├─ bool → "true"/"false"                              │
│        ├─ null → EmptyValue marker                           │
│        └─ Other → ToString()                                 │
│ 5. Join all with FieldSeparator                              │
└──────────────────────────────────────────────────────────────┘
     │
     ▼
StringForSigning: "val1|#FieldSeparator#|val2|#FieldSeparator#|..."
     │
     ▼
┌──────────────────────────────────────────────────────────────┐
│ Build Prefix                                                 │
│ "|#Length:{len};Checksum:{md5}#|"                            │
└──────────────────────────────────────────────────────────────┘
     │
     ▼
Prefix + Reverse(StringForSigning)
     │
     ▼
┌──────────────────────────────────────────────────────────────┐
│ ComputeHmacToken                                             │
│ 1. Salt: "NuciSecurity.HMAC.StaticSalt.{guid}.{input}"       │
│ 2. HMAC-SHA512(key=secret, data=salted)                      │
│ 3. Pad hash to multiple of 3 bytes (avoid Base64 '=')        │
│ 4. Base64 encode                                             │
│ 5. Replace '/'→'Л', '+'→'л'                                  │
│ 6. InvertCase()                                              │
└──────────────────────────────────────────────────────────────┘
     │
     ▼
Output Token
```

## Data Flow: Token Validation

```
ExpectedToken + Input Object + Secret
     │
     ▼
┌──────────────────────────────────────────────────────────────┐
│ HmacValidator.IsTokenValid                                   │
│ 1. If expectedToken null/empty/whitespace → return false     │
│ 2. Validate object and secret (throw if null)                │
│ 3. GenerateToken(object, secret)                             │
│ 4. String.Equals(generated, expected) → return bool          │
└──────────────────────────────────────────────────────────────┘
     │
     ▼
bool result
     │
     ├─ true  → Valid
     └─ false → Invalid
           │
           ▼
┌──────────────────────────────────────────────────────────────┐
│ HmacValidator.Validate                                       │
│ 1. Call IsTokenValid                                         │
│ 2. If false → throw SecurityException("The HMAC token...")   │
└──────────────────────────────────────────────────────────────┘
```

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Static methods only | Stateless, thread-safe, no DI needed |
| Reflection-based property enumeration | Works with any POCO, no interfaces required |
| Attributes for configuration | Declarative, discoverable, no external config |
| HMAC-SHA512 | Strong cryptographic primitive, widely available |
| Fixed salt per library | Prevents cross-library token confusion |
| String reversal + case inversion | Obscures token structure (defense in depth) |
| MD5 in prefix | Checksum only, not security-critical |
| Cyrillic substitutions | Avoids URL/encoding issues with Base64 chars |
| No '=' padding | Cleaner tokens, no padding ambiguity |
| Exact string comparison | Simple, deterministic, timing acceptable for HMAC |

## Invariants

1. **Determinism**: Same object + same secret = same token (always)
2. **Order sensitivity**: Property order affects token
3. **Exclusion**: `[HmacIgnore]` properties never affect token
4. **Null handling**: Null object throws; null property uses marker
5. **Secret validation**: Empty/whitespace secret throws
6. **Token format**: Always valid Base64-like, no `=` padding
7. **Validation symmetry**: `Validate` throws iff `IsTokenValid` returns false

## Extension Points

- **Attributes**: Only `[HmacIgnore]` and `[HmacOrder]` recognized
- **Properties**: Public instance only (no fields, private, static)
- **Collections**: Auto-detect complex vs scalar element types
- **Value formatting**: Fixed for DateTime/bool/null; ToString() for rest

## Security Boundaries

```
┌─────────────────────────────────────────────────────────────┐
│                    NuciSecurity.HMAC                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  Encoder    │  │ Validator   │  │  Attributes         │  │
│  │  (Public)   │  │  (Public)   │  │  (Public)           │  │
│  └──────┬──────┘  └──────┬──────┘  └─────────────────────┘  │
│         │                │                                    │
│         └────────┬───────┘                                    │
│                  ▼                                            │
│         ┌─────────────────┐                                   │
│         │  Internal       │                                   │
│         │  - GetStringFor │                                   │
│         │    Signing      │                                   │
│         │  - ComputeHmac  │                                   │
│         │    Token        │                                   │
│         │  - PadBytes     │                                   │
│         │  - GetMd5Hash   │                                   │
│         └────────┬────────┘                                   │
│                  │                                            │
│         ┌────────┴────────┐                                   │
│         ▼                 ▼                                   │
│  ┌─────────────┐  ┌─────────────┐                             │
│  │Reflection   │  │Cryptography │                             │
│  │(System)     │  │(System)     │                             │
│  └─────────────┘  └─────────────┘                             │
└─────────────────────────────────────────────────────────────┘
```

No internal state is mutated. All methods are pure functions of their inputs.