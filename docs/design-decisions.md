# Design Decisions

## Key Architectural and Design Choices

This document records the significant design decisions made in NuciSecurity.HMAC, including the context, alternatives considered, and rationale.

## 1. Static Class Design

### Decision
`HmacEncoder` and `HmacValidator` are static classes with stateless methods.

### Context
- Library is used for token generation/validation
- No per-instance state needed
- Methods depend only on parameters

### Alternatives Considered
1. Instance classes with constructor-injected secret
2. Interface-based design (`IHmacEncoder`, `IHmacValidator`)
3. Static classes (chosen)

### Rationale
- **Simplicity**: No instantiation needed
- **Thread safety**: Stateless methods are inherently thread-safe
- **Performance**: No object allocation per call
- **Usage pattern**: Secret passed per-call (flexible for key rotation)
- **Precedent**: Similar to `System.Convert`, `Math`, `HashAlgorithms`

### Consequences
- ✅ Simple API: `HmacEncoder.GenerateToken(obj, secret)`
- ✅ No dependency injection needed
- ✅ Thread-safe by default
- ❌ Cannot inject different implementations
- ❌ Secret must be passed each call (caller manages caching)

## 2. Attribute-Based Configuration

### Decision
Use `[HmacIgnore]` and `[HmacOrder]` attributes for declarative property configuration.

### Context
- Need to exclude certain properties from token
- Need to control property order for deterministic tokens
- Configuration should be close to the data

### Alternatives Considered
1. Fluent configuration API
2. External configuration file (JSON/XML)
3. Interface-based (`IHmacConfigurable`)
4. Attributes (chosen)

### Rationale
- **Discoverability**: Configuration visible on property
- **Reflection-friendly**: Easy to discover via `GetCustomAttributes`
- **Compile-time safety**: Invalid usage caught at compile time
- **No runtime overhead**: Attributes inspected once per type
- **Locality**: Configuration where data is defined

### Consequences
- ✅ Clear intent: `[HmacIgnore]` means "don't include in token"
- ✅ Order control: Explicit ordering values
- ✅ No magic strings: Type-safe attribute usage
- ❌ Requires `[AttributeUsage]` validation
- ❌ Properties must be public (for reflection)

## 3. Deterministic Token Generation

### Decision
Tokens are deterministic: same input → same output.

### Context
- Validation requires regenerating token for comparison
- Callers need predictable behavior
- Enables caching and pre-computation (though not needed)

### Alternatives Considered
1. Non-deterministic (random salt per token)
2. Deterministic with caller-provided salt
3. Deterministic (chosen)

### Rationale
- **Stateless validation**: No server-side state needed
- **Simplicity**: No nonce/timestamp management in library
- **Predictability**: Easy to test and debug
- **Compatibility**: Works with disconnected systems
- **Performance**: No storage/retrieval of nonces

### Consequences
- ✅ Simple validation: Regenerate and compare
- ✅ Stateless: Works in load-balanced scenarios
- ✅ Testable: Same inputs produce same outputs
- ❌ Token replay possible (mitigation: include timestamp/nonce in object)
- ❌ Requires immutable objects during token lifetime

## 4. HMAC-SHA512 as Core Algorithm

### Decision
Use HMAC-SHA512 for token generation.

### Context
- Need cryptographic integrity and authenticity
- Must resist forgery and tampering
- Should be widely available and performant

### Alternatives Considered
1. HMAC-SHA256
2. HMAC-SHA384
3. HMAC-SHA512 (chosen)
4. AES-GMAC or other MACs
5. RSA signatures (asymmetric)

### Rationale
- **Security**: 256-bit security level (resistant to quantum Grover's algorithm)
- **Performance**: Hardware-accelerated on modern CPUs (SHA extensions)
- **Availability**: In .NET since Framework 2.0
- **Standard**: RFC 2104, FIPS 198-1
- **Compatibility**: Well-understood, widely implemented

### Consequences
- ✅ Strong security: 256-bit collision resistance
- ✅ Fast execution: ~0.5-2ms typical on modern hardware
- ✅ Standard algorithm: Interoperable with other implementations
- ❌ Larger token: 88 Base64 chars vs 44 for SHA256
- ❌ Overkill for low-security applications (but future-proof)

## 5. Obfuscation Layers (Non-Security)

### Decision
Apply three obfuscation layers to the raw HMAC output:
1. String reversal (`Reverse()`)
2. Cyrillic substitution (`/`→`Л`, `+`→`л`)
3. Case inversion (`InvertCase()`)

### Context
- Raw Base64 HMAC output is recognizable
- Want to avoid accidental misuse or probing
- Defense in depth (not a security control)

### Alternatives Considered
1. No obfuscation (raw Base64)
2. Base64URL encoding
3. Custom alphabet Base64
4. Three-layer obfuscation (chosen)

### Rationale
- **Obscurity**: Tokens don't look like standard Base64/HMAC
- **Defense in depth**: Adds complexity for casual inspection
- **Compatibility**: Still valid ASCII, safe for headers/URLs
- **Reversibility**: Validation applies inverse operations
- **NuciExtensions**: Uses existing extensions from same author

### Consequences
- ✅ Less recognizable: Not obviously HMAC/Base64
- ✅ URL-safe: No `+` or `/` after obfuscation
- ✅ Case variation: Mixed case avoids case-sensitive issues
- ❌ Security through obscurity: Not a real security control
- ❌ Computational overhead: Minimal (O(n) string operations)
- ❌ Debugging harder: Tokens not immediately recognizable

## 6. MD5 Checksum in Token Prefix

### Decision
Include MD5 checksum of string-for-signing in token prefix: `|#Length:{len};Checksum:{md5}#|`

### Context
- Need to detect corruption in string-for-signing
- Want integrity check before expensive HMAC computation
- MD5 acceptable for non-cryptographic checksum

### Alternatives Considered
1. No checksum
2. SHA-256 checksum
3. Adler-32 or CRC32
4. MD5 (chosen)

### Rationale
- **Early detection**: Catch corruption before HMAC
- **Low overhead**: MD5 is fast for short strings
- **Adequate for non-crypto**: Collisions irrelevant for integrity
- **Standard**: Well-known algorithm
- **Length inclusion**: Helps detect truncation

### Consequences
- ✅ Integrity check: Detects accidental corruption
- ✅ Early failure: Avoids HMAC on corrupted input
- ✅ Length validation: Helps detect truncation/extension
- ❌ Not cryptographic: MD5 collisions possible (but irrelevant here)
- ❌ Slightly larger token: Adds prefix overhead
- ❌ False sense of security: Not for authentication

## 7. UTF8 Encoding for Secrets and Strings

### Decision
Use UTF8 encoding consistently for:
- Secret key → bytes for HMAC
- String-for-signing → bytes for MD5
- Property values → string concatenation

### Context
- Need consistent encoding across platforms
- Must handle international characters
- Should be efficient and standard

### Alternatives Considered
1. UTF8 (chosen)
2. UTF16LE (.NET default)
3. ASCII (subset)
4. Local code page

### Rationale
- **Standard**: RFC 3629, RFC 2279
- **Efficient**: ASCII characters 1 byte, others 2-4 bytes
- **Interoperable**: Same encoding used everywhere
- **Complete**: Handles all Unicode characters
- **Default in .NET**: `Encoding.UTF8` readily available

### Consequences
- ✅ Unicode support: Works with international text
- ✅ Consistent: Same encoding for secret and data
- ✅ Efficient: Compact for typical ASCII-based tokens
- ❌ Slightly larger for non-ASCII: 2-4 bytes per character
- ❌ Requires explicit encoding: Not relying on defaults

## 8. Public Instance Properties Only

### Decision
Only include public instance properties with getters in token generation.

### Context
- Reflection-based property enumeration
- Need to decide what data to include
- Should respect encapsulation

### Alternatives Considered
1. All properties (public/private, instance/static)
2. Public instance properties only (chosen)
3. Public fields and properties
4. Custom interface (`IHmacSerializable`)

### Rationale
- **Encapsulation**: Respects property accessibility
- **Consistency**: Matches typical serialization behavior
- **Safety**: No accidental inclusion of backing fields
- **Predictability**: Well-defined inclusion set
- **Performance**: Avoids expensive non-public reflection

### Consequences
- ✅ Clear contract: Only public gettable properties included
- ✅ Encapsulation respected: Private/protected excluded
- ✅ Backing fields safe: Compiler-generated fields excluded
- ❌ Cannot include computed properties without setter
- ❌ Cannot include private state intentionally
- ❌ Static properties never included (by design)

## 9. Exception Types for Error Conditions

### Decision
Use standard .NET exceptions:
- `ArgumentNullException` for null/invalid arguments
- `SecurityException` for validation failures

### Context
- Need to signal error conditions to callers
- Should use familiar exception types
- Validation failure is security-relevant

### Alternatives Considered
1. Custom exception types (`HmacValidationException`)
2. Return codes or `Try*` pattern
3. Standard exceptions (chosen)
4. `InvalidOperationException` for validation

### Rationale
- **Familiarity**: Callers know how to handle `ArgumentNullException`
- **Clarity**: `SecurityException` clearly indicates auth failure
- **No overhead**: No custom types to define/document
- **Compatibility**: Works with existing exception handling
- **Precedent**: `System.Security` uses `SecurityException`

### Consequences
- ✅ Familiar patterns: Standard exception handling
- ✅ Clear semantics: `SecurityException` = auth failure
- ✅ No extra types: Simpler API surface
- ❌ Less specific: Cannot distinguish validation failure reasons
- ❌ Caller must check message or context for details

## 10. Stateless Validation Approach

### Decision
Validation is stateless: regenerate token and compare.

### Context
- Need to validate tokens without server-side state
- Must work in scaled-out environments
- Should be simple and reliable

### Alternatives Considered
1. Server-side nonce tracking (stateful)
2. Timestamp windows (limited state)
3. Stateless regeneration (chosen)
4. Signed tokens with embedded expiration

### Rationale
- **Scalability**: No shared state needed
- **Simplicity**: No storage, expiration, or cleanup
- **Reliability**: Works after restarts, deployments
- **Performance**: No database/cache lookup
- **Compatibility**: Works with CDNs, proxies, load balancers

### Consequences
- ✅ Zero state: Works in any deployment scenario
- ✅ Simple implementation: Regenerate and compare
- ✅ No storage: No databases, caches, or files
- ❌ Token replay possible: Mitigation is application responsibility
- ❌ No built-in expiration: Must be in object if needed
- ❌ Computation cost: HMAC on every validation (but fast)

## 11. Target Framework: net10.0

### Decision
Target .NET 10.0 as the minimum framework version.

### Context
- Need to balance features, performance, and compatibility
- Should use modern .NET features
- Library is new (no legacy constraints)

### Alternatives Considered
1. .NET Standard 2.0 (maximum compatibility)
2. .NET 6.0 (LTS)
3. .NET 8.0 (LTS)
4. .NET 10.0 (chosen, current)

### Rationale
- **Modern features**: Top-level statements, global using, etc.
- **Performance**: Latest runtime optimizations
- **Language features**: C# 12+ capabilities
- **Forward-looking**: Aligns with .NET release cadence
- **Author preference**: Use latest stable

### Consequences
- ✅ Modern APIs: Access to latest .NET features
- ✅ Performance: Best possible performance
- ✅ Language: Full C# 12 feature set
- ❌ Compatibility: Requires .NET 10.0+ (no older frameworks)
- ❌ Adoption: May limit use in enterprise with older standards

## 12. GPL-3.0-or-later License

### Decision
License the library under GPL-3.0-or-later.

### Context
- Need to choose open-source license
- Must be compatible with dependencies
- Should reflect author's intentions

### Alternatives Considered
1. MIT (permissive)
2. Apache-2.0 (permissive with patent clause)
3. GPL-3.0 (copyleft)
4. LGPL-3.0 (weak copyleft)
5. GPL-3.0-or-later (chosen)

### Rationale
- **Copyleft**: Ensures derivatives remain open-source
- **Or-later**: Allows upgrading to future GPL versions
- **Compatibility**: Matches NuciExtensions (same author)
- **Ideological**: Supports free software principles
- **Community**: Encourages contribution back

### Consequences
- ✅ Freedom: Users can run, study, share, modify
- ✅ Copyleft: Derivatives must also be GPL-compatible
- ✅ Future-proof: Can upgrade to GPL-4.0 if needed
- ❌ Compatibility: Not usable in proprietary applications
- ❌ Linking restriction: GPL applies to combined work
- ❌ Enterprise adoption: May limit corporate use

## Related

- [Architecture](../architecture.md)
- [Change Guide](../change-guide.md)
- [Security Model](../security.md)
- [Ambiguities and Open Questions](../ambiguities-and-open-questions.md)