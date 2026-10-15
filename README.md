[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/nucisecurity.hmac)](https://github.com/hmlendea/nucisecurity.hmac/releases/latest)
[![Build Status](https://github.com/hmlendea/nucisecurity.hmac/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/nucisecurity.hmac/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/NuciSecurity.HMAC)](https://nuget.org/packages/NuciSecurity.HMAC)
[![License](https://img.shields.io/github/license/hmlendea/nucisecurity.hmac)](https://github.com/hmlendea/nucisecurity.hmac/blob/master/LICENSE)

# NuciSecurity.HMAC

`NuciSecurity.HMAC` is a .NET library for generating and validating deterministic HMAC tokens from object instances. It is designed for integrity and authenticity checks between trusted parties that share the same secret key (for example signed API requests, callbacks/webhooks, and anti-tampering checks for DTO payloads).

## 📑 Table of Contents

- [Capabilities](#-capabilities)
- [Usage](#-usage)
- [Installation](#-installation)
- [Development](#️-development)
- [Architecture](#️-architecture)
- [Contributing](#-contributing)
- [Code of Conduct](#-code-of-conduct)
- [Security](#-security)
- [License](#-license)
- [Project Engagement](#-project-engagement)

## ✨ Capabilities

- Deterministic token generation from object properties
- Token validation through boolean and exception-based APIs
- Property exclusion support via `[HmacIgnore]`
- Explicit property order support via `[HmacOrder]`
- Recursive support for collections of complex objects
- Stable formatting for values like `DateTime`, `bool`, and `null`

## 🚀 Usage

```csharp
using NuciSecurity.HMAC;

string sharedSecret = "super-secret-key";

var payload = new PaymentRequest
{
    MerchantId = "merchant-42",
    Amount = 125.50m,
    Currency = "EUR",
    NonSignedMetadata = "ignore-me"
};

// Generate a token to transmit with the payload
string token = HmacEncoder.GenerateToken(payload, sharedSecret);

// Validate by returning true/false
bool isValid = HmacValidator.IsTokenValid(token, payload, sharedSecret);

// Or validate by throwing SecurityException when invalid
HmacValidator.Validate(token, payload, sharedSecret);

public sealed class PaymentRequest
{
    [HmacOrder(1)]
    public string MerchantId { get; set; }

    [HmacOrder(2)]
    public decimal Amount { get; set; }

    [HmacOrder(3)]
    public string Currency { get; set; }

    [HmacIgnore]
    public string NonSignedMetadata { get; set; }
}
```

## 📦 Installation

[![Obtain it from NuGet](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/nuget.png)](https://nuget.org/packages/NuciSecurity.HMAC)

### Package Manager Installation

```bash
dotnet add package NuciSecurity.HMAC
```

Or, via the `Package Manager Console`:
```powershell
Install-Package NuciSecurity.HMAC
```

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Setup

```bash
git clone https://github.com/hmlendea/nucisecurity.hmac.git
cd nucisecurity.hmac
dotnet restore
```

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

## 🏗️ Architecture

NuciSecurity.HMAC provides deterministic HMAC token generation and validation for .NET applications.

See the [architecture documentation](./ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Preserve the existing public contract unless a breaking change is intentional
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Include tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/nucisecurity.hmac/issues) for problems or suggestions

See the [contributing guidelines](./CONTRIBUTING.md) for details on how to report issues, suggest enhancements, and submit changes.

## 📜 Code of Conduct

See the [Code of Conduct](./CODE_OF_CONDUCT.md) for the standards of conduct expected in all project spaces.

## 🔒 Security

NuciSecurity.HMAC follows a coordinated disclosure policy for security vulnerabilities.

For information on reporting security vulnerabilities, see [SECURITY.md](./SECURITY.md).

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for additional information.

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/nucisecurity.hmac/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)
