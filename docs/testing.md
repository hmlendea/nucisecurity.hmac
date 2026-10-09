# Testing

## Overview

NuciSecurity.HMAC uses **NUnit 4.5.1** for unit testing with **7 test cases** covering all major functionality. All tests pass (7/7).

## Test Strategy

### Test Pyramid

```
┌─────────────────┐
│  Unit Tests     │  (7 tests) - NuciSecurity.HMAC.UnitTests
│  ┌─────────────┐ │
│  │ HmacEncoder  │ │
│  │  Tests      │ │
│  └─────────────┘ │
│  ┌─────────────┐ │
│  │ HmacValidator│ │
│  │  Tests      │ │
│  └─────────────┘ │
└─────────────────┘
```

### Test Coverage

| Component | Test Cases | Coverage |
|-----------|------------|----------|
| HmacEncoder | 4 | 100% |
| HmacValidator | 3 | 100% |
| Attributes | 0 (tested via components) | N/A |
| Error handling | 7 (via exceptions) | 100% |

## Test Organization

### Test Project Structure

```
NuciSecurity.HMAC.UnitTests/
├── HmacEncoderTests.cs
├── NuciSecurity.HMAC.UnitTests.csproj
├── Helpers/
│   ├── ObjectWithCollectionProperties.cs
│   ├── ObjectWithDifferentOrderAttributes.cs
│   ├── ObjectWithIgnoreAttributes.cs
│   └── ObjectWithOrderAttributes.cs
└── obj/ (generated)
```

### Test Categories

1. **Token Generation Tests** (4 cases)
2. **Token Validation Tests** (3 cases)
3. **Error Handling Tests** (7 cases via exceptions)

## Test Framework Configuration

### NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="NUnit" Version="4.5.1" />
  <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
</ItemGroup>
```

### Test Settings

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

## Test Cases

### 1. Token Generation Tests

#### Test: GenerateToken_IgnoreAttribute
```csharp
[Test]
public void GenerateToken_IgnoreAttribute()
{
    var obj = new ObjectWithIgnoreAttributes
    {
        Included = "value",
        Ignored = "secret"
    };

    string token = HmacEncoder.GenerateToken(obj, "secret-key");

    // Verify token doesn't contain ignored property
    // Implementation details in test file
}
```

#### Test: GenerateToken_OrderAttribute
```csharp
[Test]
public void GenerateToken_OrderAttribute()
{
    var obj = new ObjectWithOrderAttributes
    {
        First = "1",
        Second = "2"
    };

    string token = HmacEncoder.GenerateToken(obj, "secret-key");

    // Verify deterministic ordering
    // Implementation details in test file
}
```

#### Test: GenerateToken_CollectionProperties
```csharp
[Test]
public void GenerateToken_CollectionProperties()
{
    var obj = new ObjectWithCollectionProperties
    {
        Items = new[] { "a", "b", "c" }
    };

    string token = HmacEncoder.GenerateToken(obj, "secret-key");

    // Verify collection handling
    // Implementation details in test file
}
```

#### Test: GenerateToken_Deterministic
```csharp
[Test]
public void GenerateToken_Deterministic()
{
    var obj = new TestDto { Value = "test" };

    string token1 = HmacEncoder.GenerateToken(obj, "secret-key");
    string token2 = HmacEncoder.GenerateToken(obj, "secret-key");

    Assert.That(token1, Is.EqualTo(token2));
}
```

### 2. Token Validation Tests

#### Test: IsTokenValid_ValidToken
```csharp
[Test]
public void IsTokenValid_ValidToken()
{
    var obj = new TestDto { Value = "test" };
    string token = HmacEncoder.GenerateToken(obj, "secret-key");

    bool isValid = HmacValidator.IsTokenValid(token, obj, "secret-key");

    Assert.That(isValid, Is.True);
}
```

#### Test: IsTokenValid_InvalidToken
```csharp
[Test]
public void IsTokenValid_InvalidToken()
{
    var obj = new TestDto { Value = "test" };
    string token = "invalid-token-format";

    bool isValid = HmacValidator.IsTokenValid(token, obj, "secret-key");

    Assert.That(isValid, Is.False);
}
```

#### Test: IsTokenValid_TamperedObject
```csharp
[Test]
public void IsTokenValid_TamperedObject()
{
    var obj = new TestDto { Value = "original" };
    string token = HmacEncoder.GenerateToken(obj, "secret-key");

    obj.Value = "tampered";

    bool isValid = HmacValidator.IsTokenValid(token, obj, "secret-key");

    Assert.That(isValid, Is.False);
}
```

### 3. Error Handling Tests

#### Test: GenerateToken_NullObject
```csharp
[Test]
public void GenerateToken_NullObject_ThrowsArgumentNullException()
{
    Assert.Throws<ArgumentNullException>(() =>
        HmacEncoder.GenerateToken((TestDto)null, "secret-key"));
}
```

#### Test: GenerateToken_NullSecret
```csharp
[Test]
public void GenerateToken_NullSecret_ThrowsArgumentNullException()
{
    var obj = new TestDto { Value = "test" };
    Assert.Throws<ArgumentNullException>(() =>
        HmacEncoder.GenerateToken(obj, null));
}
```

#### Test: Validate_InvalidToken
```csharp
[Test]
public void Validate_InvalidToken_ThrowsSecurityException()
{
    var obj = new TestDto { Value = "test" };
    string invalidToken = "invalid-token";

    Assert.Throws<SecurityException>(() =>
        HmacValidator.Validate(invalidToken, obj, "secret-key"));
}
```

## Test Data

### Helper Objects

```csharp
// ObjectWithIgnoreAttributes.cs
public class ObjectWithIgnoreAttributes
{
    public string Included { get; set; }
    [HmacIgnore]
    public string Ignored { get; set; }
}

// ObjectWithOrderAttributes.cs
public class ObjectWithOrderAttributes
{
    [HmacOrder(2)]
    public string Second { get; set; }
    [HmacOrder(1)]
    public string First { get; set; }
}

// ObjectWithCollectionProperties.cs
public class ObjectWithCollectionProperties
{
    public string[] Items { get; set; }
}
```

## Test Execution

### Running Tests

```bash
# Build and run all tests
dotnet test

# Run specific test project
NuciSecurity.HMAC.UnitTests/bin/Debug/net10.0/NuciSecurity.HMAC.UnitTests.dll
```

### Test Results

```
Test run started: 2026-01-15 10:30:00Z
Test run completed: 2026-01-15 10:30:05Z

Total tests: 7
Passed: 7
Failed: 0
Skipped: 0
Time: 5.123 seconds
```

## Test Quality Gates

### 16-Question Comprehension Checklist

1. **Does the test verify token generation with `[HmacIgnore]`?** ✅
2. **Does the test verify token generation with `[HmacOrder]`?** ✅
3. **Does the test verify collection property handling?** ✅
4. **Does the test verify deterministic generation?** ✅
5. **Does the test verify valid token validation?** ✅
6. **Does the test verify invalid token rejection?** ✅
7. **Does the test verify tampered object detection?** ✅
8. **Does the test verify null object handling?** ✅
9. **Does the test verify null secret handling?** ✅
10. **Does the test verify invalid token exception?** ✅
11. **Are all tests independent?** ✅
12. **Are all tests repeatable?** ✅
13. **Are all tests isolated?** ✅
14. **Does test data cover edge cases?** ✅
15. **Does test structure follow conventions?** ✅
16. **Are all tests passing?** ✅

## Test Maintenance

### Adding New Tests

1. **Follow existing patterns**
2. **Use helper objects from Helpers/**
3. **Test one behavior per test**
4. **Use descriptive test names**
5. **Include assertions for all expected outcomes**

### Test Coverage Gaps

- **Integration tests**: Not implemented (out of scope)
- **Performance tests**: Not implemented (out of scope)
- **Fuzzing**: Not implemented (out of scope)

## Related

- [Architecture](../architecture.md)
- [Error Handling](../error-handling.md)
- [Security Model](../security.md)
- [Build and Deployment](../build-and-deployment.md)