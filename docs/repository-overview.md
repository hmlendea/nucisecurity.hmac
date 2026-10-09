# Repository Overview

## Purpose

NuciSecurity.HMAC is a .NET library for generating and validating deterministic HMAC-SHA512 tokens from object instances. It enables secure request authentication between trusted parties sharing a secret key.

## Scope

### In Scope
- HMAC token generation from .NET objects
- HMAC token validation (boolean and exception-based)
- Attribute-driven property configuration (`[HmacIgnore]`, `[HmacOrder]`)
- Deterministic token generation (same input → same output)
- Reflection-based property enumeration
- Integration with .NET ecosystem

### Out of Scope
- Key management (storage, rotation, distribution)
- Replay protection (nonce/timestamp tracking)
- Asymmetric cryptography (digital signatures)
- Token storage or caching
- Network transport (HTTP, gRPC, etc.)
- User authentication/authorization
- Session management

## Entry Points

### Primary API

```csharp
// Generate token from object
string token = HmacEncoder.GenerateToken(payload, secret);

// Validate token (boolean)
bool isValid = HmacValidator.IsTokenValid(token, payload, secret);

// Validate token (exception)
HmacValidator.Validate(token, payload, secret);
```

### Configuration Attributes

```csharp
[HmacIgnore]           // Exclude property from token
[HmacOrder(1)]         // Control property order
```

## Target Audience

- .NET developers implementing request authentication
- Teams building microservices with shared secrets
- Applications requiring tamper-proof request validation
- Systems needing stateless authentication

## Use Cases

### 1. API Request Authentication

```
Client                          Server
  │                                │
  ├─ GenerateToken(request) ──────►│
  │                                │
  ├─ POST /api + token ───────────►│
  │                                ├─ IsTokenValid(token, request)
  │                                │
  │◄──── 200 OK ───────────────────┤
```

### 2. Message Queue Integrity

```
Publisher                       Consumer
  │                                │
  ├─ GenerateToken(message) ──────►│
  │                                │
  ├─ Publish + token ─────────────►│
  │                                ├─ IsTokenValid(token, message)
```

### 3. Inter-Service Communication

```
Service A                       Service B
  │                                │
  ├─ GenerateToken(payload) ──────►│
  │                                │
  ├─ gRPC/HTTP + token ───────────►│
  │                                ├─ Validate(token, payload)
```

## Key Features

| Feature | Description |
|---------|-------------|
| Deterministic | Same object + secret → same token |
| Attribute-driven | `[HmacIgnore]`, `[HmacOrder]` on properties |
| Reflection-based | Automatic property discovery |
| Collection support | Arrays, lists, enumerables |
| Thread-safe | Stateless, concurrent access |
| No dependencies | Only NuciExtensions (string extensions) |
| GPL-3.0 licensed | Open source, copyleft |

## Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| Performance | < 2ms per token (typical) |
| Memory | < 5 KB allocation per call |
| Thread safety | Fully thread-safe |
| Compatibility | .NET 10.0+ |
| Security | HMAC-SHA512 (256-bit security) |

## Project Metadata

| Property | Value |
|----------|-------|
| Name | NuciSecurity.HMAC |
| Version | 4.1.3 |
| Target Framework | net10.0 |
| License | GPL-3.0-or-later |
| Author | Horațiu Mlendea |
| Repository | https://github.com/hmlendea/nucisecurity |
| NuGet | NuciSecurity.HMAC |

## Related

- [Architecture](../architecture.md)
- [Quick Start](../quick-start.md)
- [Repository Structure](../repository-structure.md)
- [Dependencies](../dependencies.md)