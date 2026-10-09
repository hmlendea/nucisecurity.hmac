# Integrations

## Overview

NuciSecurity.HMAC integrates with various frameworks and platforms. This document covers common integration patterns.

## ASP.NET Core

### Middleware

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

// Registration
app.UseMiddleware<HmacValidationMiddleware>();
```

### Action Filter

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ValidateHmacAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context,
                                             ActionExecutionDelegate next)
    {
        var config = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>();
        var secret = config["Hmac:Secret"];

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

### Minimal API

```csharp
app.MapPost("/api/payments", (PaymentRequest request,
    [FromHeader] string xHmacToken,
    IConfiguration config) =>
{
    var secret = config["Hmac:Secret"];

    if (!HmacValidator.IsTokenValid(xHmacToken, request, secret))
        return Results.Unauthorized();

    return Results.Ok(ProcessPayment(request));
});
```

## gRPC

### Client Interceptor

```csharp
public class HmacClientInterceptor : Interceptor
{
    private readonly string _secret;

    public HmacClientInterceptor(string secret)
    {
        _secret = secret;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        string token = HmacEncoder.GenerateToken(request, _secret);

        var headers = new Metadata { { "x-hmac-token", token } };
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, headers);

        return continuation(request, newContext);
    }
}

// Registration
var channel = GrpcChannel.ForAddress("https://api.example.com",
    new GrpcChannelOptions
    {
        Interceptors = { new HmacClientInterceptor(secret) }
    });
```

### Server Interceptor

```csharp
public class HmacServerInterceptor : Interceptor
{
    private readonly string _secret;

    public HmacServerInterceptor(string secret)
    {
        _secret = secret;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var token = context.RequestHeaders.GetValue("x-hmac-token");

        if (!HmacValidator.IsTokenValid(token, request, _secret))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid HMAC token"));
        }

        return await continuation(request, context);
    }
}

// Registration
var server = new Server
{
    Services = { MyService.BindService(new MyServiceImpl()).Intercept(new HmacServerInterceptor(secret)) },
    Ports = { new ServerPort("localhost", 5000, ServerCredentials.Insecure) }
};
```

## HttpClient

### DelegatingHandler

```csharp
public class HmacDelegatingHandler : DelegatingHandler
{
    private readonly string _secret;

    public HmacDelegatingHandler(string secret)
    {
        _secret = secret;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<PaymentRequest>(content);

            string token = HmacEncoder.GenerateToken(payload, _secret);
            request.Headers.Add("X-HMAC-Token", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

// Registration
services.AddHttpClient<IPaymentClient, PaymentClient>()
    .AddHttpMessageHandler<HmacDelegatingHandler>();
```

## Azure Functions

### HTTP Trigger

```csharp
public class PaymentFunction
{
    private readonly string _secret;

    public PaymentFunction(IConfiguration config)
    {
        _secret = config["Hmac:Secret"]
            ?? throw new InvalidOperationException("HMAC secret not configured");
    }

    [FunctionName("ReceivePayment")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "payments")]
        HttpRequest req, ILogger log)
    {
        var token = req.Headers["X-HMAC-Token"].FirstOrDefault();
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<PaymentRequest>(body);

        if (!HmacValidator.IsTokenValid(token, request, _secret))
        {
            return new UnauthorizedResult();
        }

        return new OkObjectResult(await ProcessPaymentAsync(request));
    }
}
```

## AWS Lambda

### API Gateway

```csharp
public class PaymentFunction
{
    private readonly string _secret;

    public PaymentFunction()
    {
        _secret = Environment.GetEnvironmentVariable("HMAC_SECRET")
            ?? throw new InvalidOperationException("HMAC secret not configured");
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request, ILambdaContext context)
    {
        var token = request.Headers.GetValueOrDefault("X-HMAC-Token");
        var payload = JsonSerializer.Deserialize<PaymentRequest>(request.Body);

        if (!HmacValidator.IsTokenValid(token, payload, _secret))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 401,
                Body = "Invalid HMAC token"
            };
        }

        var result = await ProcessPaymentAsync(payload);
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(result)
        };
    }
}
```

## Message Queues

### RabbitMQ

```csharp
public class HmacMessagePublisher
{
    private readonly IModel _channel;
    private readonly string _secret;

    public HmacMessagePublisher(IConnection connection, string secret)
    {
        _channel = connection.CreateModel();
        _secret = secret;
    }

    public void Publish<T>(T message, string exchange, string routingKey)
    {
        string token = HmacEncoder.GenerateToken(message, _secret);

        var properties = _channel.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>
        {
            ["x-hmac-token"] = token
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        _channel.BasicPublish(exchange, routingKey, properties, body);
    }
}

public class HmacMessageConsumer
{
    private readonly string _secret;

    public HmacMessageConsumer(string secret)
    {
        _secret = secret;
    }

    public bool Validate(BasicDeliverEventArgs args)
    {
        var token = args.BasicProperties.Headers?["x-hmac-token"] as string;
        var body = Encoding.UTF8.GetString(args.Body.Span);
        var message = JsonSerializer.Deserialize<PaymentRequest>(body);

        return HmacValidator.IsTokenValid(token, message, _secret);
    }
}
```

### Azure Service Bus

```csharp
public class HmacServiceBusSender
{
    private readonly ServiceBusSender _sender;
    private readonly string _secret;

    public HmacServiceBusSender(ServiceBusSender sender, string secret)
    {
        _sender = sender;
        _secret = secret;
    }

    public async Task SendAsync<T>(T message)
    {
        string token = HmacEncoder.GenerateToken(message, _secret);

        var sbMessage = new ServiceBusMessage(JsonSerializer.Serialize(message))
        {
            ApplicationProperties = { ["x-hmac-token"] = token }
        };

        await _sender.SendMessageAsync(sbMessage);
    }
}
```

## Docker/Kubernetes

### Environment Variable

```dockerfile
# Dockerfile
ENV HMAC_SECRET=""
```

```yaml
# k8s deployment
env:
  - name: HMAC_SECRET
    valueFrom:
      secretKeyRef:
        name: hmac-secret
        key: secret
```

### Sidecar Pattern

```yaml
# Sidecar for secret rotation
containers:
  - name: app
    image: myapp
    env:
      - name: HMAC_SECRET
        valueFrom:
          secretKeyRef:
            name: hmac-secret
            key: current
  - name: secret-rotator
    image: secret-rotator
    volumeMounts:
      - name: hmac-secret
        mountPath: /secrets
volumes:
  - name: hmac-secret
    secret:
      secretName: hmac-secret
```

## Testing Integrations

### Test Server (ASP.NET Core)

```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _secret = "test-secret";

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Hmac:Secret", _secret)
                });
            });
        });
    }

    [Fact]
    public async Task ValidToken_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var request = new PaymentRequest { Amount = 100m };
        string token = HmacEncoder.GenerateToken(request, _secret);

        client.DefaultRequestHeaders.Add("X-HMAC-Token", token);
        var response = await client.PostAsJsonAsync("/api/payments", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

## Related

- [Quick Start](../quick-start.md)
- [Configuration](../configuration.md)
- [Security Model](../security.md)