# Privacy and Personal Data

NuciSecurity.HMAC is a .NET library for generating and validating deterministic HMAC tokens from object instances. It is distributed as a NuGet package and runs entirely within the consuming application's process. No personal data is collected, transmitted, or stored by the library itself.

**Information reviewed:** 2026-10-12

## 📑 Table of Contents

- [What This Document Covers](#-what-this-document-covers)
- [Self-Hosted Deployments](#-self-hosted-deployments)
- [Data We Handle](#-data-we-handle)
- [Processing and Use](#-processing-and-use)
- [Storage, Retention, and Deletion](#-storage-retention-and-deletion)
- [External Processing and Integrations](#-external-processing-and-integrations)
- [Document Changes](#-document-changes)
- [Contact](#-contact)

## 🔎 What This Document Covers

This document describes how NuciSecurity.HMAC at https://github.com/hmlendea/nucisecurity handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

NuciSecurity.HMAC is a library consumed via NuGet and runs entirely within the host application's process. The library maintainers do not operate any service, collect any data, or receive any data from self-hosted instances. Instance operators (application developers who include this library) control all configuration, local storage, logs, backups, access controls, retention, and request handling for their applications. The library itself has no telemetry, update checks, crash reporting, email, authentication, reverse-proxy, object-storage, or monitoring integrations that transmit data to the project maintainers or external services.

## 📥 Data We Handle

### Data Provided to the Application

The library processes object instances provided by the consuming application to generate or validate HMAC tokens. The data categories depend entirely on what the consuming application chooses to sign. The library does not request, collect, or store any personal data on its own.

### Data Generated or Collected by the Application

The library generates HMAC tokens (Base64-encoded strings) from the provided object data and a shared secret key. No personal data is generated, collected, logged, or stored by the library automatically. Tokens are returned to the caller and not retained.

### Data Received from Integrations

The library has no built-in integrations that receive personal data from third parties. It depends on NuciExtensions (a utility library by the same author) for string manipulation helpers, but no data is transmitted to or received from NuciExtensions.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- HMAC token generation — Object properties provided by the caller (excluding those marked with `[HmacIgnore]`)
- HMAC token validation — Object properties provided by the caller and the expected token string

## 🗄️ Storage, Retention, and Deletion

The library does not store any data. Tokens are computed in memory and returned to the caller. The consuming application controls all storage, retention, and deletion of tokens and signed objects. For self-hosted deployments, the instance operator controls storage, deletion, and backups of any data their application handles.

## 🔗 External Processing and Integrations

The application has no built-in external data transfer. The only dependency is NuciExtensions (https://www.nuget.org/packages/NuciExtensions), which provides string manipulation utilities (string reversal, case inversion) used for token obfuscation. No data is transmitted to NuciExtensions or any external service.

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| NuciExtensions | String manipulation utilities (reversal, case inversion) for token obfuscation | None (in-process string operations) | https://www.nuget.org/packages/NuciExtensions |

## 🛡️ Data Protection and Security

The library implements HMAC-SHA512 with a static salt for domain separation. Security relies on the secrecy of the shared key provided by the consuming application. The library performs no key management; the consuming application is responsible for secure key generation, storage, and rotation. Token obfuscation (string reversal, Cyrillic substitution, case inversion) is applied for encoding safety, not security. For self-hosted deployments, the instance operator is responsible for updates, secrets management, access controls, backups, network exposure, and log protection in their application.

## 🔄 Document Changes

Update this document when application data flows, storage, integrations, or deployment responsibilities change. The current version is published at https://github.com/hmlendea/nucisecurity/blob/main/PRIVACY.md.

## 📬 Contact

For questions about application data handling, contact the project maintainers at https://github.com/hmlendea/nucisecurity/issues. For a self-hosted instance, contact the instance operator, unless the project explicitly handles the request. Include the library version and deployment context; do not send passwords, access tokens, or other secrets.