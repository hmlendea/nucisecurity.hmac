# Logging

## Overview

NuciSecurity.HMAC **does not include a logging framework**. It is a library that leaves logging to the consuming application. This document describes recommended logging practices for applications using this library.

## Logging Framework

### Recommended: Microsoft.Extensions.Logging

```csharp
using Microsoft.Extensions.Logging;

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly string _hmacSecret;

    public PaymentService(ILogger<PaymentService> logger, IConfiguration config)
    {
        _logger = logger;
        _hmacSecret = config["Hmac:Secret"]
            ?? throw new InvalidOperationException("HMAC secret not configured");
    }
}
```

## What to Log

### Token Generation

```csharp
public string GeneratePaymentToken(PaymentRequest request)
{
    _logger.LogDebug("Generating HMAC token for merchant {MerchantId}", request.MerchantId);

    try
    {
        string token = HmacEncoder.GenerateToken(request, _hmacSecret);
        _logger.LogInformation("HMAC token generated for merchant {MerchantId}", request.MerchantId);
        return token;
    }
    catch (ArgumentNullException ex)
    {
        _logger.LogError(ex, "Failed to generate HMAC token: {ParamName} is null", ex.ParamName);
        throw;
    }
}
```

### Token Validation

```csharp
public bool ValidatePaymentToken(string token, PaymentRequest request)
{
    _logger.LogDebug("Validating HMAC token for merchant {MerchantId}", request.MerchantId);

    try
    {
        bool isValid = HmacValidator.IsTokenValid(token, request, _hmacSecret);

        if (isValid)
        {
            _logger.LogInformation("HMAC token valid for merchant {MerchantId}", request.MerchantId);
        }
        else
        {
            _logger.LogWarning("HMAC token invalid for merchant {MerchantId}", request.MerchantId);
        }

        return isValid;
    }
    catch (ArgumentNullException ex)
    {
        _logger.LogError(ex, "HMAC validation configuration error: {ParamName} is null", ex.ParamName);
        throw;
    }
}
```

### Exception-Based Validation

```csharp
public void ValidateOrThrow(string token, PaymentRequest request)
{
    _logger.LogDebug("Validating HMAC token (exception mode) for merchant {MerchantId}", request.MerchantId);

    try
    {
        HmacValidator.Validate(token, request, _hmacSecret);
        _logger.LogInformation("HMAC token valid for merchant {MerchantId}", request.MerchantId);
    }
    catch (SecurityException ex)
    {
        _logger.LogWarning(ex, "HMAC token invalid for merchant {MerchantId}", request.MerchantId);
        throw;
    }
    catch (ArgumentNullException ex)
    {
        _logger.LogError(ex, "HMAC validation configuration error: {ParamName} is null", ex.ParamName);
        throw;
    }
}
```

## What NOT to Log

### Never Log Secrets

```csharp
// NEVER
_logger.LogInformation("Using secret: {Secret}", _hmacSecret);
_logger.LogDebug("Secret length: {Length}", _hmacSecret.Length);

// NEVER
_logger.LogInformation("Token: {Token}", token);
_logger.LogDebug("Token preview: {Preview}", token[..10]);
```

### Never Log Full Objects with Sensitive Data

```csharp
// NEVER - may contain PII, credentials, etc.
_logger.LogInformation("Request: {@Request}", request);

// INSTEAD - log only safe identifiers
_logger.LogInformation("Processing request for merchant {MerchantId}, order {OrderId}",
    request.MerchantId, request.OrderId);
```

## Structured Logging Fields

### Recommended Fields

| Field | Type | Description |
|-------|------|-------------|
| `MerchantId` | string | Merchant identifier |
| `OrderId` | string | Order identifier |
| `Operation` | string | "GenerateToken" or "ValidateToken" |
| `Result` | string | "Success", "Invalid", "Error" |
| `DurationMs` | long | Operation duration |
| `ErrorType` | string | Exception type (if error) |

### Example Structured Log

```csharp
using System.Diagnostics;

public async Task<bool> ValidateAsync(string token, PaymentRequest request)
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        bool isValid = HmacValidator.IsTokenValid(token, request, _hmacSecret);

        _logger.LogInformation(
            "HMAC validation completed | MerchantId={MerchantId} OrderId={OrderId} Operation=ValidateToken Result={Result} DurationMs={DurationMs}",
            request.MerchantId, request.OrderId, isValid ? "Success" : "Invalid", stopwatch.ElapsedMilliseconds);

        return isValid;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "HMAC validation failed | MerchantId={MerchantId} OrderId={OrderId} Operation=ValidateToken Result=Error ErrorType={ErrorType} DurationMs={DurationMs}",
            request.MerchantId, request.OrderId, ex.GetType().Name, stopwatch.ElapsedMilliseconds);
        throw;
    }
}
```

## Log Levels

| Level | Usage |
|-------|-------|
| `Trace` | Detailed token generation steps (verbose) |
| `Debug` | Token generation/validation entry/exit |
| `Information` | Successful operations, audit trail |
| `Warning` | Invalid tokens, configuration issues |
| `Error` | Exceptions, configuration failures |
| `Critical` | Secret unavailable, system misconfiguration |

## Correlation IDs

```csharp
public async Task<IActionResult> ProcessPayment([FromHeader] string correlationId, PaymentRequest request)
{
    using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        _logger.LogInformation("Processing payment request");

        string token = GeneratePaymentToken(request);
        bool isValid = ValidatePaymentToken(token, request);

        return isValid ? Ok() : Unauthorized();
    }
}
```

## Sinks and Outputs

### Console (Development)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Console": {
      "FormatterName": "json",
      "IncludeScopes": true
    }
  }
}
```

### File (Production)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "File": {
      "Path": "logs/hmac-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  }
}
```

### Structured (Production)

```csharp
// Serilog example
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "PaymentService")
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.Seq("https://seq.example.com")
    .CreateLogger();
```

## Performance Considerations

### Avoid Allocation in Hot Paths

```csharp
// GOOD: Use interpolated strings (no allocation if not logged)
_logger.LogDebug("Generating token for {MerchantId}", request.MerchantId);

// BAD: String concatenation always allocates
_logger.LogDebug("Generating token for " + request.MerchantId);
```

### Conditional Logging

```csharp
// GOOD: Check level before expensive operations
if (_logger.IsEnabled(LogLevel.Debug))
{
    var details = ComputeExpensiveDebugInfo(request);
    _logger.LogDebug("Token details: {Details}", details);
}
```

## Audit Logging

For compliance, log all token operations:

```csharp
public class HmacAuditLogger
{
    private readonly ILogger _logger;

    public void LogTokenGenerated(string merchantId, string operation, string tokenId)
    {
        _logger.LogInformation(
            "AUDIT: HMAC token generated | MerchantId={MerchantId} Operation={Operation} TokenId={TokenId} Timestamp={Timestamp}",
            merchantId, operation, tokenId, DateTimeOffset.UtcNow);
    }

    public void LogTokenValidated(string merchantId, string operation, bool isValid)
    {
        _logger.LogInformation(
            "AUDIT: HMAC token validated | MerchantId={MerchantId} Operation={Operation} Valid={Valid} Timestamp={Timestamp}",
            merchantId, operation, isValid, DateTimeOffset.UtcNow);
    }
}
```

## Related

- [Error Handling](../error-handling.md)
- [Security Model](../security.md)
- [Configuration](../configuration.md)