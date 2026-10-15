# Security

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.
<!-- `Latest version` is literal text and must not be replaced with a version number. -->

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Latest version | NuGet | ✅ |
| Latest version | conda-forge | ❌ |
| Latest version | unofficial third-party distribution channels | ❌ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/nucisecurity.hmac/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The subsequent report categories are in scope for this repository:
- HMAC token generation and validation
- Property attribute configuration ([HmacOrder], [HmacIgnore])

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Cryptographic key generation or management
- Underlying .NET runtime security

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.

<!-- Only if a safe-harbour statement is available. -->
## 🧾 Safe Harbour

If your research is conducted in good faith, confined to authorised scope, and disclosed responsibly, the maintainers will not pursue action for policy-compliant activity.

<!-- Only if public recognition is practised. -->
## 🙏 Recognition

We appreciate responsible disclosure. Reporters who desire public attribution may be acknowledged in release notes, advisories, or a dedicated acknowledgements section.