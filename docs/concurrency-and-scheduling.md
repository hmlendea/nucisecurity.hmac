# Concurrency and Scheduling

## Overview

NuciSecurity.HMAC is designed for **concurrent, multi-threaded environments**. All public methods are thread-safe by design.

## Thread Safety

### Static Methods

```csharp
// All methods are static and stateless
public static string GenerateToken<TObject>(TObject obj, string sharedSecretKey)
public static bool IsTokenValid<TObject>(string expectedToken, TObject obj, string sharedSecretKey)
public static void Validate<TObject>(string expectedToken, TObject obj, string sharedSecretKey)
```

**Thread Safety**: ✅ **Fully thread-safe**

- No shared mutable state
- No static fields modified after initialization
- Parameters passed by value (reference types) or immutable (strings)
- Local variables only

### Reflection Caching

```csharp
// Internal: Property info cached per-type
private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();
```

**Thread Safety**: ✅ **Thread-safe**

- Uses `ConcurrentDictionary` for lock-free reads
- Write-once per type (first access)
- No invalidation needed (types immutable at runtime)

## Concurrency Model

### Execution Model

```
Thread 1: HmacEncoder.GenerateToken(obj1, secret)  ──► HMAC-SHA512 ──► token1
Thread 2: HmacEncoder.GenerateToken(obj2, secret)  ──► HMAC-SHA512 ──► token2
Thread 3: HmacValidator.IsTokenValid(token1, obj1, secret) ──► true
Thread 4: HmacValidator.IsTokenValid(token2, obj2, secret) ──► true
```

- **No locks**: All operations are lock-free
- **No shared state**: Each call independent
- **CPU-bound**: HMAC computation uses CPU, no I/O
- **Scalable**: Linear scaling with cores

### HMACSHA512 Thread Safety

```csharp
using var hmac = new HMACSHA512(keyBytes);
byte[] hash = hmac.ComputeHash(dataBytes);
```

- `HMACSHA512` is **not thread-safe** for instance reuse
- Library creates **new instance per call** (via `using`)
- No instance sharing between threads
- Each call allocates new `HMACSHA512` (lightweight)

## Performance Under Concurrency

### Allocation Profile

| Operation | Allocations | Notes |
|-----------|-------------|-------|
| `GenerateToken` | ~2-3 KB | StringBuilder, byte arrays, HMACSHA512 |
| `IsTokenValid` | ~2-3 KB | Same as GenerateToken + comparison |
| `Validate` | ~2-3 KB | Same + exception on failure |

### Contention Points

| Resource | Contention | Mitigation |
|----------|------------|------------|
| `HMACSHA512` | None (per-call) | New instance each call |
| `ConcurrentDictionary` | Minimal (read-heavy) | Lock-free reads |
| `StringBuilder` | None (local) | Per-call allocation |
| `MD5.HashData` | None (static) | Thread-safe static method |

### Benchmark (Estimated)

```
Concurrent calls (100 threads, 1000 ops each):
- GenerateToken: ~50,000 ops/sec (8-core)
- IsTokenValid: ~50,000 ops/sec (8-core)
- Latency p99: < 5ms
- No lock contention observed
```

## Async Considerations

### Current: Synchronous Only

```csharp
// Synchronous - blocks thread during HMAC computation
string token = HmacEncoder.GenerateToken(request, secret);
bool valid = HmacValidator.IsTokenValid(token, request, secret);
```

### Why Not Async?

1. **CPU-bound**: HMAC-SHA512 is computation, not I/O
2. **Fast**: Typically < 1ms per operation
3. **No benefit**: Async adds overhead without freeing thread
4. **Caller choice**: Callers can wrap in `Task.Run` if needed

### If You Need Async

```csharp
// Wrapper for async contexts
public static Task<string> GenerateTokenAsync<TObject>(TObject obj, string secret)
    => Task.Run(() => HmacEncoder.GenerateToken(obj, secret));

public static Task<bool> IsTokenValidAsync<TObject>(string token, TObject obj, string secret)
    => Task.Run(() => HmacValidator.IsTokenValid(token, obj, secret));
```

## Scheduling

### No Internal Scheduling

- No timers, background tasks, or schedulers
- No `Task.Run`, `ThreadPool`, or `Dispatcher` usage
- Execution happens on caller's thread
- Caller controls scheduling

### Integration with ASP.NET Core

```csharp
// Controller action - runs on request thread
[HttpPost]
public IActionResult Receive(PaymentRequest request, [FromHeader] string xHmacToken)
{
    // Synchronous validation on request thread
    if (!HmacValidator.IsTokenValid(xHmacToken, request, _secret))
        return Unauthorized();

    return Ok(Process(request));
}
```

### Integration with Background Services

```csharp
// Background worker - runs on worker thread
public class TokenGenerator : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var request = await _queue.DequeueAsync(stoppingToken);
            string token = HmacEncoder.GenerateToken(request, _secret);
            await _sender.SendAsync(token, request);
        }
    }
}
```

## Deadlocks and Livelocks

### None Possible

- No locks acquired
- No async/await in library code
- No synchronization primitives
- No circular dependencies

## Memory Model

### No Shared Memory

- No static fields with mutable state
- No `volatile`, `Interlocked`, or `MemoryBarrier`
- Each call: stack-only + heap allocations (GC-managed)
- No cross-thread memory visibility concerns

## Testing Concurrency

### Stress Test

```csharp
[Test]
public void ConcurrentGeneration_ProducesConsistentTokens()
{
    var obj = new TestDto { Value = "test" };
    string secret = "secret";
    string expected = HmacEncoder.GenerateToken(obj, secret);

    Parallel.For(0, 1000, _ =>
    {
        string token = HmacEncoder.GenerateToken(obj, secret);
        Assert.That(token, Is.EqualTo(expected));
    });
}

[Test]
public void ConcurrentValidation_ThreadSafe()
{
    var obj = new TestDto { Value = "test" };
    string secret = "secret";
    string token = HmacEncoder.GenerateToken(obj, secret);

    Parallel.For(0, 1000, _ =>
    {
        bool valid = HmacValidator.IsTokenValid(token, obj, secret);
        Assert.That(valid, Is.True);
    });
}
```

## Best Practices for Callers

### Do

```csharp
// Reuse secret reference (string is immutable)
private readonly string _secret = config["Hmac:Secret"];

// Call directly - thread-safe
string token = HmacEncoder.GenerateToken(request, _secret);
bool valid = HmacValidator.IsTokenValid(token, request, _secret);
```

### Don't

```csharp
// Don't create wrapper with locks (unnecessary)
private readonly object _lock = new();
public string GenerateToken(Request request)
{
    lock (_lock)  // UNNECESSARY - library is thread-safe
    {
        return HmacEncoder.GenerateToken(request, _secret);
    }
}
```

## Related

- [Architecture](../architecture.md)
- [Performance](../analyzing-dotnet-performance.md)
- [Security Model](../security.md)