# Quick Start

## Installation

```bash
dotnet add package NuciSecurity.HMAC
```

## Basic Usage

### 1. Define Your Payload

```csharp
public class PaymentRequest
{
    public string MerchantId { get; set; }
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD"

    // Exclude from token
    [HmacIgnore]
    public string InternalNotes { get; set; }

    // Control order
    [HmacOrder(1)]
    public string MerchantId { get; set; }

    [HmacOrder(2)]
    public string OrderId { get; set; }
}
```

### 2. Generate Token (Sender)

```csharp
using NuciSecurity.HMAC;

var request = new PaymentRequest
{
    MerchantId = "MERCH_123",
    OrderId = "ORD_456",
    Amount = 99.99m,
    Currency = "USD"
};

string secret = GetSecretFromSecureStore(); // Your secret management
string token = HmacEncoder.GenerateToken(request, secret);

// Send token + request to receiver
await SendToReceiverAsync(token, request);
```

### 3. Validate Token (Receiver)

```csharp
using NuciSecurity.HMAC;

bool isValid = HmacValidator.IsTokenValid(receivedToken, receivedRequest, secret);

if (!isValid)
{
    return Unauthorized(); // 401
}

// Process valid request
await ProcessPaymentAsync(receivedRequest);
```

## Complete Example

### Sender Side

```csharp
public class PaymentSender
{
    private readonly string _hmacSecret;
    private readonly HttpClient _httpClient;

    public PaymentSender(IConfiguration config, HttpClient httpClient)
    {
        _hmacSecret = config["Hmac:Secret"]
            ?? throw new InvalidOperationException("HMAC secret not configured");
        _httpClient = httpClient;
    }

    public async Task<PaymentResponse> SendPaymentAsync(PaymentRequest request)
    {
        // Generate HMAC token
        string token = HmacEncoder.GenerateToken(request, _hmacSecret);

        // Send with token in header
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-HMAC-Token", token);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PaymentResponse>();
    }
}
```

### Receiver Side

```csharp
public class PaymentReceiver
{
    private readonly string _hmacSecret;

    public PaymentReceiver(IConfiguration config)
    {
        _hmacSecret = config["Hmac:Secret"]
            ?? throw new InvalidOperationException("HMAC secret not configured");
    }

    public async Task<IActionResult> ReceivePayment([FromBody] PaymentRequest request,
                                                     [FromHeader] string xHmacToken)
    {
        // Validate token
        if (!HmacValidator.IsTokenValid(xHmacToken, request, _hmacSecret))
        {
            return Unauthorized();
        }

        // Process payment
        var result = await ProcessPaymentAsync(request);
        return Ok(result);
    }
}
```

## Configuration

### appsettings.json

```json
{
  "Hmac": {
    "Secret": "your-cryptographically-random-secret-here"
  }
}
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Validate secret at startup
string hmacSecret = builder.Configuration["Hmac:Secret"];
if (string.IsNullOrWhiteSpace(hmacSecret))
{
    throw new InvalidOperationException("HMAC secret not configured");
}

builder.Services.AddSingleton(hmacSecret);
builder.Services.AddHttpClient<PaymentSender>();

var app = builder.Build();
app.MapPost("/api/payments", async (PaymentRequest request,
    [FromHeader] string xHmacToken, string secret) =>
{
    if (!HmacValidator.IsTokenValid(xHmacToken, request, secret))
        return Results.Unauthorized();

    return Results.Ok(await ProcessPaymentAsync(request));
});

app.Run();
```

## Common Patterns

### Pattern: Middleware Validation

```csharp
public class HmacValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _secret;

    public HmacValidationMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _secret = config["Hmac:Secret"]
            ?? throw new InvalidOperationException("HMAC secret not configured");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-HMAC-Token", out var token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing HMAC token");
            return;
        }

        // Read body for validation
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        var request = JsonSerializer.Deserialize<PaymentRequest>(body);

        if (!HmacValidator.IsTokenValid(token, request, _secret))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid HMAC token");
            return;
        }

        await _next(context);
    }
}
```

### Pattern: Attribute-Based Validation

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class ValidateHmacAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context,
                                             ActionExecutionDelegate next)
    {
        var secret = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Hmac:Secret"];

        var token = context.HttpContext.Request.Headers["X-HMAC-Token"].FirstOrDefault();
        var request = context.ActionArguments.Values.FirstOrDefault();

        if (request == null || !HmacValidator.IsTokenValid(token, request, secret))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}

// Usage
[HttpPost]
[ValidateHmac]
public IActionResult ReceivePayment(PaymentRequest request) => Ok();
```

## Testing

```csharp
[Test]
public void QuickStart_GenerateAndValidate()
{
    var request = new PaymentRequest
    {
        MerchantId = "MERCH_123",
        OrderId = "ORD_456",
        Amount = 99.99m
    };

    string secret = "test-secret";

    // Generate
    string token = HmacEncoder.GenerateToken(request, secret);

    // Validate
    bool isValid = HmacValidator.IsTokenValid(token, request, secret);

    Assert.That(isValid, Is.True);
}
```

## Next Steps

- [Configuration](../configuration.md) — Detailed configuration options
- [API Reference](../api-reference/INDEX.md) — Complete API documentation
- [Security Model](../security.md) — Security best practices
- [Error Handling](../error-handling.md) — Exception handling patterns