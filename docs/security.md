# Security Model

This document complements the root [SECURITY.md](../SECURITY.md) with implementation-level security details.

## Threat Model

### Assets
- **Shared secret key**: Must remain confidential between trusted parties
- **Token integrity**: Tokens must not be forgeable without the secret
- **Object integrity**: Signed objects must not be tampered with

### Threats
| Threat | Mitigation |
|--------|------------|
| Token forgery | HMAC-SHA512 with secret key |
| Token replay | Include timestamp/nonce in signed object |
| Secret leakage | Never log tokens or secrets |
| Object tampering | All properties included in token (except ignored) |
| Order manipulation | `[HmacOrder]` ensures deterministic ordering |

### Trust Boundaries
```
┌─────────────────┐    ┌─────────────────┐
│  Trusted Party  │    │  Trusted Party  │
│  (Sender)       │    │  (Receiver)     │
│                 │    │                 │
│  GenerateToken  │───►│  Validate       │
│  (has secret)   │    │  (has secret)   │
└─────────────────┘    └─────────────────┘
       │                      │
       ▼                      ▼
  ┌─────────────────────────────────┐
  │        Untrusted Transport      │
  │  (token + object in transit)    │
  └─────────────────────────────────┘
```

## Cryptographic Design

### Algorithm: HMAC-SHA512

- **Standard**: RFC 2104
- **Hash function**: SHA-512 (512-bit output)
- **Key**: UTF8-encoded shared secret
- **Security level**: 256-bit (half of hash output)

### Salt

```csharp
private static string StaticSalt =>
    "NuciSecurity.HMAC.StaticSalt.8fc5307e-c10b-40d0-b710-de79e7954358";
```

**Purpose**: Domain separation
- Prevents token reuse across different libraries
- Prevents cross-library token confusion
- Fixed per-library (not per-token)

### MD5 Checksum (Prefix)

```csharp
private static string GetMd5Hash(string input)
{
    byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
    // ...
}
```

**Purpose**: Integrity check in prefix
- **Not security-critical**: MD5 is cryptographically broken
- **Usage**: Checksum only, not for authentication
- **Location**: Prefix format `|#Length:{len};Checksum:{md5}#|`

### Token Obfuscation

Three layers of obfuscation (not security):

1. **String Reversal**: `stringForSigning.Reverse()`
   - Reverses the string-for-signing before HMAC
   - From NuciExtensions

2. **Cyrillic Substitution**: `/`→`Л`, `+`→`л`
   - Replaces Base64 special characters
   - Avoids URL/encoding issues

3. **Case Inversion**: `InvertCase()`
   - Flips case of each character
   - From NuciExtensions

**Note**: These are obfuscation, not encryption. Security relies on HMAC-SHA512.

## Secret Key Management

### Requirements

| Requirement | Detail |
|-------------|--------|
| Length | Minimum 128 bits (16 bytes) recommended |
| Entropy | Cryptographically random |
| Storage | Secure vault (Azure Key Vault, AWS Secrets Manager, etc.) |
| Rotation | Periodic (e.g., every 90 days) |
| Distribution | Secure channel only |

### Best Practices

```csharp
// GOOD: Load from secure configuration
string secret = configuration["Hmac:Secret"]
    ?? throw new InvalidOperationException("HMAC secret not configured");

// BAD: Hardcoded in source
string secret = "hardcoded-secret";  // NEVER

// BAD: Logged
logger.LogInformation("Using secret: {Secret}", secret);  // NEVER
```

### Key Rotation

```csharp
// Support multiple secrets during rotation
public class HmacService
{
    private readonly List<string> _secrets;  // Current + previous

    public bool Validate(string token, object payload)
    {
        foreach (var secret in _secrets)
        {
            if (HmacValidator.IsTokenValid(token, payload, secret))
                return true;
        }
        return false;
    }
}
```

## Token Security Properties

### Determinism
- Same object + same secret → same token
- Enables stateless validation
- **Risk**: Token reuse (mitigate with timestamps/nonces)

### Unforgeability
- HMAC-SHA512 ensures only secret holders can generate valid tokens
- 256-bit security level
- No known practical attacks

### Integrity
- All included properties contribute to token
- Any change → different token
- `[HmacIgnore]` properties excluded (by design)

### Non-repudiation
- **Not provided**: HMAC is symmetric (both parties have secret)
- For non-repudiation, use digital signatures (asymmetric)

## Validation Security

### Comparison Method

```csharp
return HmacEncoder.GenerateToken(obj, sharedSecretKey).Equals(expectedToken);
```

- **Ordinal comparison**: Exact match required
- **Case-sensitive**: Token case matters
- **Not constant-time**: Acceptable for HMAC (attacker knows token)

### Timing Attacks

- **Risk**: Low — attacker already knows the token
- **Mitigation**: Not needed for HMAC validation
- **Note**: If token is secret, use constant-time comparison

## Data Exposure

### What's in the Token
- HMAC-SHA512 hash (64 bytes → 88 Base64 chars)
- No plaintext data
- No object property values
- No secret key material

### What's NOT in the Token
- Original object data
- Secret key
- Timestamps (unless included in object)

### Logging

```csharp
// NEVER log tokens or secrets
logger.LogWarning("HMAC validation failed");  // OK
logger.LogWarning("Token: {Token}", token);  // NEVER
logger.LogWarning("Secret: {Secret}", secret);  // NEVER
```

## Attack Vectors

### 1. Token Forgery
- **Attack**: Generate valid token without secret
- **Mitigation**: HMAC-SHA512 (computationally infeasible)
- **Status**: Secure

### 2. Token Replay
- **Attack**: Reuse captured token
- **Mitigation**: Include timestamp/nonce in signed object
- **Status**: Application responsibility

### 3. Secret Compromise
- **Attack**: Attacker obtains secret key
- **Mitigation**: Key rotation, secure storage
- **Status**: Operational responsibility

### 4. Object Tampering
- **Attack**: Modify object properties
- **Mitigation**: All properties in token (except ignored)
- **Status**: Secure (by design)

### 5. Order Manipulation
- **Attack**: Reorder properties to change token
- **Mitigation**: `[HmacOrder]` ensures deterministic ordering
- **Status**: Secure (by design)

## Security Testing

### Test Cases

```csharp
// Token forgery resistance
[Test]
public void TokenCannotBeForgedWithoutSecret()
{
    var obj = new TestDto { Value = "test" };
    string token = HmacEncoder.GenerateToken(obj, "correct-secret");

    // Different secret → different token
    Assert.That(
        HmacValidator.IsTokenValid(token, obj, "wrong-secret"),
        Is.False);
}

// Tamper detection
[Test]
public void TamperedObjectDetected()
{
    var obj = new TestDto { Value = "original" };
    string token = HmacEncoder.GenerateToken(obj, "secret");

    obj.Value = "tampered";

    Assert.That(
        HmacValidator.IsTokenValid(token, obj, "secret"),
        Is.False);
}

// Ignored property doesn't affect token
[Test]
public void IgnoredPropertyChangeDoesNotAffectToken()
{
    var obj1 = new TestDto { Value = "test", Ignored = "A" };
    var obj2 = new TestDto { Value = "test", Ignored = "B" };

    string token1 = HmacEncoder.GenerateToken(obj1, "secret");
    string token2 = HmacEncoder.GenerateToken(obj2, "secret");

    Assert.That(token1, Is.EqualTo(token2));
}
```

## Compliance

### Standards
- **HMAC**: RFC 2104
- **SHA-512**: FIPS 180-4
- **Base64**: RFC 4648

### Certifications
- None (library, not certified product)

## Related

- [Architecture](../architecture.md)
- [Error Handling](../error-handling.md)
- [Invariants](../invariants.md)
- [Root SECURITY.md](../SECURITY.md)