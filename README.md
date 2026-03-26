[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html) [![Build Status](https://github.com/hmlendea/nucisecurity/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/nucisecurity/actions/workflows/dotnet.yml) [![Latest Release](https://img.shields.io/github/v/release/hmlendea/nucisecurity)](https://github.com/hmlendea/nucisecurity/releases/latest)

# NuciSecurity.HMAC

## About

`NuciSecurity.HMAC` is a .NET library for generating and validating deterministic HMAC tokens from object instances.

It is designed for integrity and authenticity checks between trusted parties that share the same secret key (for example signed API requests, callbacks/webhooks, and anti-tampering checks for DTO payloads).

## Features

- Deterministic token generation from object properties
- Token validation through boolean and exception-based APIs
- Property exclusion support via `[HmacIgnore]`
- Explicit property order support via `[HmacOrder]`
- Recursive support for collections of complex objects
- Stable formatting for values like `DateTime`, `bool`, and `null`

## Installation

[![Get it from NuGet](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/nuget.png)](https://nuget.org/packages/NuciSecurity.HMAC)

### NET CLI
```bash
dotnet add package NuciSecurity.HMAC
```

### Package Manager
```powershell
Install-Package NuciSecurity.HMAC
```

## Quick Start

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

## API Overview

- `HmacEncoder.GenerateToken<T>(T obj, string sharedSecretKey)`
	- Creates a token from object content and secret key.
	- Throws if `obj` is null.
	- Throws if `sharedSecretKey` is null, empty, or whitespace.

- `HmacValidator.IsTokenValid<T>(string expectedToken, T obj, string sharedSecretKey)`
	- Returns `false` if `expectedToken` is null, empty, or whitespace.
	- Otherwise generates a new token and compares it with `expectedToken`.

- `HmacValidator.Validate<T>(string expectedToken, T obj, string sharedSecretKey)`
	- Throws `SecurityException` when the token is invalid.

- `HmacIgnoreAttribute`
	- Excludes a property from the token input.

- `HmacOrderAttribute(int order)`
	- Defines property processing order during token generation.

Note: `HmacEncoder.IsTokenValid(...)` is obsolete. Prefer `HmacValidator.IsTokenValid(...)`.

## Behaviour Notes

- Token generation is deterministic for identical object values and shared secret.
- Property order impacts the resulting token.
	- Properties with `[HmacOrder]` are processed first by order value.
	- Remaining public instance properties are processed alphabetically by name.
- `[HmacIgnore]` properties are excluded.
- Value normalization details:
	- `DateTime` uses the round-trip format (`"O"`).
	- `bool` is lowercased (`"true"` / `"false"`).
	- `null` is represented through an internal marker.
- Collections:
	- Collections of complex objects are processed recursively.
	- Collections of scalar/simple values are flattened in sequence order.
- Validation is an exact string comparison between expected and generated token.

## Target Framework

The package currently targets `.NET 10.0`.

## Testing

Unit tests are available in the `NuciSecurity.HMAC.UnitTests` project.

Run all tests using:

```bash
dotnet test
```

## License

This project is licensed under the `GNU General Public License v3.0` or later. See [LICENSE](./LICENSE) for details.
