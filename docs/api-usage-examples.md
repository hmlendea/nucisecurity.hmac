# API Usage Examples

## Quick Start

```csharp
using NuciSecurity.HMAC;

string secret = "shared-secret-key";

var payload = new PaymentRequest
{
    MerchantId = "merchant-42",
    Amount = 125.50m,
    Currency = "EUR"
};

string token = HmacEncoder.GenerateToken(payload, secret);
bool valid = HmacValidator.IsTokenValid(token, payload, secret);
```

---

## Complete Examples

### 1. Basic DTO with Attributes

```csharp
using NuciSecurity.HMAC;

public sealed class PaymentRequest
{
    [HmacOrder(1)]
    public string MerchantId { get; set; }

    [HmacOrder(2)]
    public decimal Amount { get; set; }

    [HmacOrder(3)]
    public string Currency { get; set; }

    [HmacIgnore]
    public string InternalNotes { get; set; }
}

string secret = "super-secret-key";

var request = new PaymentRequest
{
    MerchantId = "merchant-42",
    Amount = 125.50m,
    Currency = "EUR",
    InternalNotes = "This will be ignored"
};

string token = HmacEncoder.GenerateToken(request, secret);
Console.WriteLine($"Token: {token}");

// Later, validate
bool isValid = HmacValidator.IsTokenValid(token, request, secret);
Console.WriteLine($"Valid: {isValid}");  // true
```

### 2. Complex Object with Collections

```csharp
using NuciSecurity.HMAC;
using System.Collections.Generic;

public sealed class Order
{
    [HmacOrder(1)]
    public string OrderId { get; set; }

    [HmacOrder(2)]
    public List<OrderItem> Items { get; set; }

    [HmacOrder(3)]
    public List<string> Tags { get; set; }

    [HmacIgnore]
    public DateTime CreatedAt { get; set; }
}

public sealed class OrderItem
{
    [HmacOrder(1)]
    public string ProductId { get; set; }

    [HmacOrder(2)]
    public int Quantity { get; set; }

    [HmacOrder(3)]
    public decimal UnitPrice { get; set; }

    [HmacIgnore]
    public string InternalSku { get; set; }
}

string secret = "order-secret";

var order = new Order
{
    OrderId = "ORD-12345",
    Items = new List<OrderItem>
    {
        new() { ProductId = "PROD-001", Quantity = 2, UnitPrice = 29.99m, InternalSku = "SKU-001" },
        new() { ProductId = "PROD-002", Quantity = 1, UnitPrice = 49.99m, InternalSku = "SKU-002" }
    },
    Tags = new List<string> { "electronics", "sale", "priority" },
    CreatedAt = DateTime.UtcNow
};

string token = HmacEncoder.GenerateToken(order, secret);
bool valid = HmacValidator.IsTokenValid(token, order, secret);
```

### 3. API Request/Response Signing

```csharp
using NuciSecurity.HMAC;
using System.Net.Http;
using System.Text.Json;

public sealed class ApiRequest
{
    [HmacOrder(1)]
    public string Endpoint { get; set; }

    [HmacOrder(2)]
    public string Method { get; set; }

    [HmacOrder(3)]
    public string Payload { get; set; }

    [HmacOrder(4)]
    public long Timestamp { get; set; }
}

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly string _secret;

    public ApiClient(HttpClient http, string secret)
    {
        _http = http;
        _secret = secret;
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        var payload = JsonSerializer.Serialize(request);
        var apiRequest = new ApiRequest
        {
            Endpoint = endpoint,
            Method = "POST",
            Payload = payload,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        string token = HmacEncoder.GenerateToken(apiRequest, _secret);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("X-HMAC-Token", token);
        httpRequest.Headers.Add("X-HMAC-Payload", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload)));

        var response = await _http.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseContent);
    }
}
```

### 4. Webhook Verification

```csharp
using NuciSecurity.HMAC;
using Microsoft.AspNetCore.Mvc;

public sealed class WebhookPayload
{
    [HmacOrder(1)]
    public string EventType { get; set; }

    [HmacOrder(2)]
    public string EventId { get; set; }

    [HmacOrder(3)]
    public string Data { get; set; }

    [HmacOrder(4)]
    public long Timestamp { get; set; }
}

[ApiController]
[Route("webhooks")]
public class WebhookController : ControllerBase
{
    private readonly string _webhookSecret;

    public WebhookController(IConfiguration config)
    {
        _webhookSecret = config["WebhookSecret"];
    }

    [HttpPost]
    public IActionResult Receive(
        [FromBody] WebhookPayload payload,
        [FromHeader(Name = "X-HMAC-Signature")] string signature)
    {
        if (!HmacValidator.IsTokenValid(signature, payload, _webhookSecret))
        {
            return Unauthorized();
        }

        // Process verified webhook
        ProcessEvent(payload);
        return Ok();
    }

    private void ProcessEvent(WebhookPayload payload)
    {
        // Handle event
    }
}
```

### 5. DTO Anti-Tampering for Storage

```csharp
using NuciSecurity.HMAC;
using System.Text.Json;

public sealed class StoredRecord
{
    [HmacOrder(1)]
    public string Id { get; set; }

    [HmacOrder(2)]
    public string Data { get; set; }

    [HmacOrder(3)]
    public int Version { get; set; }

    [HmacIgnore]
    public string HmacToken { get; set; }
}

public class SecureStorage
{
    private readonly string _secret;

    public SecureStorage(string secret) => _secret = secret;

    public string Serialize(StoredRecord record)
    {
        record.HmacToken = HmacEncoder.GenerateToken(record, _secret);
        return JsonSerializer.Serialize(record);
    }

    public StoredRecord Deserialize(string json)
    {
        var record = JsonSerializer.Deserialize<StoredRecord>(json);

        if (!HmacValidator.IsTokenValid(record.HmacToken, record, _secret))
        {
            throw new SecurityException("Stored record has been tampered with");
        }

        return record;
    }
}
```

### 6. Using Validate for Fail-Fast

```csharp
using NuciSecurity.HMAC;
using System.Security;

public void ProcessPayment(PaymentRequest request, string token)
{
    try
    {
        HmacValidator.Validate(token, request, _secret);
    }
    catch (SecurityException)
    {
        _logger.LogWarning("Invalid HMAC token for payment {MerchantId}", request.MerchantId);
        throw new UnauthorizedAccessException("Invalid payment signature");
    }

    // Process payment...
}
```

### 7. Testing Token Stability

```csharp
using NuciSecurity.HMAC;
using NUnit.Framework;

[TestFixture]
public class TokenStabilityTests
{
    private const string Secret = "test-secret";

    [Test]
    public void SameObjectProducesSameToken()
    {
        var obj = new TestDto { Id = 1, Name = "Test", Value = 42 };

        string token1 = HmacEncoder.GenerateToken(obj, Secret);
        string token2 = HmacEncoder.GenerateToken(obj, Secret);

        Assert.That(token1, Is.EqualTo(token2));
    }

    [Test]
    public void IgnoredPropertyChangeDoesNotAffectToken()
    {
        var obj1 = new TestDto { Id = 1, Name = "Test", Internal = "A" };
        var obj2 = new TestDto { Id = 1, Name = "Test", Internal = "B" };

        string token1 = HmacEncoder.GenerateToken(obj1, Secret);
        string token2 = HmacEncoder.GenerateToken(obj2, Secret);

        Assert.That(token1, Is.EqualTo(token2));
    }

    [Test]
    public void OrderChangeProducesDifferentToken()
    {
        var obj1 = new OrderedDto { First = "A", Second = "B" };
        var obj2 = new ReorderedDto { First = "A", Second = "B" };

        string token1 = HmacEncoder.GenerateToken(obj1, Secret);
        string token2 = HmacEncoder.GenerateToken(obj2, Secret);

        Assert.That(token1, Is.Not.EqualTo(token2));
    }

    public sealed class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [HmacIgnore]
        public string Internal { get; set; }
    }

    public sealed class OrderedDto
    {
        [HmacOrder(1)]
        public string First { get; set; }

        [HmacOrder(2)]
        public string Second { get; set; }
    }

    public sealed class ReorderedDto
    {
        [HmacOrder(2)]
        public string First { get; set; }

        [HmacOrder(1)]
        public string Second { get; set; }
    }
}
```

---

## Integration Patterns

### ASP.NET Core Middleware

```csharp
public class HmacAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _secret;

    public HmacAuthenticationMiddleware(RequestDelegate next, string secret)
    {
        _next = next;
        _secret = secret;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-HMAC-Token", out var token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing HMAC token");
            return;
        }

        // Read and deserialize body
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        var payload = JsonSerializer.Deserialize<dynamic>(body);

        if (!HmacValidator.IsTokenValid(token, payload, _secret))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid HMAC token");
            return;
        }

        await _next(context);
    }
}
```

### Minimal API

```csharp
app.MapPost("/api/secure", (SecurePayload payload, string token, string secret) =>
{
    if (!HmacValidator.IsTokenValid(token, payload, secret))
        return Results.Unauthorized();

    return Results.Ok(Process(payload));
});
```

---

## Error Handling Examples

```csharp
try
{
    string token = HmacEncoder.GenerateToken(payload, secret);
}
catch (ArgumentNullException ex) when (ex.ParamName == "obj")
{
    // Handle null payload
}
catch (ArgumentNullException ex) when (ex.ParamName == "sharedSecretKey")
{
    // Handle missing secret
}

try
{
    HmacValidator.Validate(token, payload, secret);
}
catch (SecurityException)
{
    // Invalid token
}
catch (ArgumentNullException ex)
{
    // Null payload or secret
}
```