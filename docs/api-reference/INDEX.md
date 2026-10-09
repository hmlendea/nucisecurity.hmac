# API Reference Index

## Endpoint Summary

| Method | Path | Controller | Action | Auth |
|--------|------|------------|--------|------|
| `GenerateToken` | N/A (static method) | `HmacEncoder` | `GenerateToken<T>` | None |
| `IsTokenValid` | N/A (static method) | `HmacValidator` | `IsTokenValid<T>` | None |
| `Validate` | N/A (static method) | `HmacValidator` | `Validate<T>` | None |

## Controllers

### HmacEncoder
**File**: [hmac-encoder.md](./hmac-encoder.md)
**Base**: Static class, no base path
**Methods**:
- `GenerateToken<TObject>(TObject obj, string sharedSecretKey)` — Generate HMAC token
- `IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey)` — [Obsolete] Validate token

### HmacValidator
**File**: [hmac-validator.md](./hmac-validator.md)
**Base**: Static class, no base path
**Methods**:
- `IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey)` — Validate token (bool)
- `Validate<TObject>(string expectedToken, TObject obj, string sharedSecretKey)` — Validate token (throws)

## Attributes

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `HmacIgnoreAttribute` | Property | Exclude property from token |
| `HmacOrderAttribute` | Property | Specify property order in token |

## Global Authentication

No authentication required for library methods. The shared secret key serves as the authentication credential between parties.

## Versioning

- **Library Version**: 4.1.3 (in csproj)
- **Target Framework**: .NET 10.0
- **Breaking Changes**: Major version increments
- **Obsolete Members**: Marked with `[Obsolete]` attribute, retained for at least one major version

## Rate Limiting

Not applicable — this is a library, not a service. Consumers implement their own rate limiting.

## Related Documentation

- [Architecture](./architecture.md) — System architecture
- [Data Model](./data-model.md) — Object model and value normalization
- [Flows: Token Generation](./flows/token-generation.md) — Generation execution flow
- [Flows: Token Validation](./flows/token-validation.md) — Validation execution flow
- [Behaviour: Token Generation](./behaviour/token-generation.md) — Usage patterns
- [Behaviour: Token Validation](./behaviour/token-validation.md) — Usage patterns