# Security Policy

This document describes the security policy for NuciSecurity.HMAC, including supported versions, vulnerability reporting, scope, disclosure practices, and automated scanning.

## 📑 Table of Contents

- [Supported Versions](#-supported-versions)
- [Reporting a Vulnerability](#-reporting-a-vulnerability)
- [Scope](#-scope)
- [Disclosure Policy](#-disclosure-policy)
- [Automated Security Scanning](#-automated-security-scanning)
- [Safe Harbour](#-safe-harbour)
- [Recognition](#-recognition)

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

The original maintainers are not responsible for changes, modifications, or security issues introduced by downstream forks.

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

## 🔍 Automated Security Scanning

This repository runs automated security scans via GitHub Actions. The scan includes:
- **Dependency vulnerability scanning** — `dotnet list package --vulnerable --include-transitive` checks all direct and transitive NuGet dependencies for known vulnerabilities
- **CodeQL static analysis** — GitHub's CodeQL engine analyses the C# codebase for security issues including injection flaws, insecure configurations, and other code-level vulnerabilities

Results are published to the [Security](https://github.com/hmlendea/nucisecurity.hmac/security) tab and the [Actions](https://github.com/hmlendea/nucisecurity.hmac/actions/workflows/security.yml) workflow page.

## 🧾 Safe Harbour

If your investigation is conducted in good faith, confined to authorised scope, and disclosed responsibly, the maintainers will not pursue action for policy-compliant activity.

## 🙏 Recognition

We appreciate responsible disclosure. Reporters who desire public attribution may be acknowledged in release notes, advisories, or a dedicated acknowledgements section.