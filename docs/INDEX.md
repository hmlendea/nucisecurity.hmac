# NuciSecurity.HMAC Documentation Index

## Repository Summary

**NuciSecurity.HMAC** is a .NET library for generating and validating deterministic HMAC tokens from object instances. It provides integrity and authenticity checks between trusted parties that share the same secret key (e.g., signed API requests, callbacks/webhooks, anti-tampering checks for DTO payloads).

- **Target Framework**: .NET 10.0
- **License**: GPL-3.0-or-later
- **Package**: [NuciSecurity.HMAC on NuGet](https://nuget.org/packages/NuciSecurity.HMAC)
- **Repository**: https://github.com/hmlendea/nucisecurity

---

## Root Document Map

| Document | Location | Purpose |
|----------|----------|---------|
| Architecture | [ARCHITECTURE.md](../ARCHITECTURE.md) | High-level architecture overview |
| Security | [SECURITY.md](../SECURITY.md) | Security model and threat mitigations |
| Roadmap | [ROADMAP.md](../ROADMAP.md) | Future development plans |
| Privacy | [PRIVACY.md](../PRIVACY.md) | Data handling and privacy |
| License | [LICENSE](../LICENSE) | GNU GPL v3.0 license text |

---

## Documentation Catalogue

### Architecture & Design
| File | Description |
|------|-------------|
| [architecture.md](./architecture.md) | Detailed architecture complementing root ARCHITECTURE.md |
| [design-decisions.md](./design-decisions.md) | Key architectural choices and rationale |
| [components/hmac-encoder.md](./components/hmac-encoder.md) | HmacEncoder deep dive |
| [components/hmac-validator.md](./components/hmac-validator.md) | HmacValidator deep dive |
| [components/attributes.md](./components/attributes.md) | HmacIgnoreAttribute and HmacOrderAttribute |

### API Reference
| File | Description |
|------|-------------|
| [api-reference/INDEX.md](./api-reference/INDEX.md) | API reference index with endpoint summary |
| [api-reference/hmac-encoder.md](./api-reference/hmac-encoder.md) | HmacEncoder API documentation |
| [api-reference/hmac-validator.md](./api-reference/hmac-validator.md) | HmacValidator API documentation |

### Execution Flows
| File | Description |
|------|-------------|
| [flows/token-generation.md](./flows/token-generation.md) | End-to-end token generation flow |
| [flows/token-validation.md](./flows/token-validation.md) | End-to-end token validation flow |

### Behaviour & Usage
| File | Description |
|------|-------------|
| [behaviour/token-generation.md](./behaviour/token-generation.md) | Token generation behaviour and examples |
| [behaviour/token-validation.md](./behaviour/token-validation.md) | Token validation behaviour and examples |
| [api-usage-examples.md](./api-usage-examples.md) | Complete usage examples |

### Technical Reference
| File | Description |
|------|-------------|
| [data-model.md](./data-model.md) | Object model, property handling, value normalization |
| [invariants.md](./invariants.md) | System-wide invariants and contracts |
| [dependencies.md](./dependencies.md) | External and internal dependencies |
| [configuration.md](./configuration.md) | Configuration schema and precedence |
| [error-handling.md](./error-handling.md) | Error taxonomy and handling patterns |
| [security.md](./security.md) | Security model (complements root SECURITY.md) |
| [testing.md](./testing.md) | Test strategy, organisation, coverage |
| [build-and-deployment.md](./build-and-deployment.md) | Build pipeline and deployment |
| [logging.md](./logging.md) | Logging framework and practices |

### Operations
| File | Description |
|------|-------------|
| [quick-start.md](./quick-start.md) | Getting started guide |
| [troubleshooting.md](./troubleshooting.md) | Common issues and solutions |
| [change-guide.md](./change-guide.md) | How to modify common areas safely |
| [documentation-maintenance.md](./documentation-maintenance.md) | Keeping docs current |
| [faq.md](./faq.md) | Frequently asked questions |
| [ambiguities-and-open-questions.md](./ambiguities-and-open-questions.md) | Unresolved items and known gaps |

---

## Navigation Aids

### Start Here (Newcomers)
1. [Quick Start](./quick-start.md) — Get running in 5 minutes
2. [API Usage Examples](./api-usage-examples.md) — Copy-paste examples
3. [Behaviour: Token Generation](./behaviour/token-generation.md) — Understand what tokens look like

### Deep Dive (Component Owners)
1. [Architecture](./architecture.md) — System structure
2. [Components](./components/) — Per-component deep dives
3. [Design Decisions](./design-decisions.md) — Why things are the way they are

### Flows (Debuggers)
1. [Token Generation Flow](./flows/token-generation.md) — Step-by-step execution trace
2. [Token Validation Flow](./flows/token-validation.md) — Step-by-step validation trace

---

## Maintenance Metadata

- **Last Reviewed**: 2026-10-09
- **Coverage Status**: Complete for v4.1.3
- **Repository Version**: 4.1.3