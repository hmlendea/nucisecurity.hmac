# Data Model

## Overview

The library operates on **arbitrary POCO objects** via reflection. No interfaces, base classes, or attributes are required. Attributes (`[HmacIgnore]`, `[HmacOrder]`) provide optional declarative configuration.

## Object Model

```
┌─────────────────────────────────────────────────────────┐
│                    Any POCO Object                      │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Public Instance Properties                        │ │
│  │  ┌──────────────────────────────────────────────┐  │ │
│  │  │  [HmacIgnore] → EXCLUDED from token          │  │ │
│  │  │  [HmacOrder(n)] → Ordered by n, then by name   │  │ │
│  │  │  No attribute → Default order (int.MaxValue)   │  │ │
│  │  └──────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

## Property Selection

### Included Properties
- **Public** instance properties only
- **BindingFlags**: `Public | Instance`
- **Any type**: primitives, strings, complex objects, collections

### Excluded Properties
- Private properties
- Static properties
- Properties with `[HmacIgnore]`
- Non-public fields (not properties)

### Property Ordering

```
1. Properties with [HmacOrder] → sorted by Order ascending
2. Properties without [HmacOrder] → sorted by Name alphabetically
3. Tie-breaker: alphabetical by property name
```

**Default order**: `int.MaxValue` (all unordered properties processed last)

## Value Normalization

### Primitive Types

| Type | Normalization | Example |
|------|---------------|---------|
| `string` | As-is | `"hello"` |
| `int` | `ToString()` | `"42"` |
| `long` | `ToString()` | `"1234567890"` |
| `decimal` | `ToString()` | `"125.50"` |
| `double` | `ToString()` | `"3.14159"` |
| `float` | `ToString()` | `"2.5"` |
| `bool` | `ToLowerInvariant()` | `"true"` / `"false"` |
| `DateTime` | `ToString("O")` | `"2026-10-09T12:30:45.1234567Z"` |
| `DateTimeOffset` | `ToString()` | `"10/9/2026 12:30:45 PM +00:00"` |
| `Guid` | `ToString()` | `"a1b2c3d4-e5f6-7890-abcd-ef1234567890"` |
| `Enum` | `ToString()` | `"Pending"` |
| `null` | `EmptyValue` marker | `"|#EmptyValue#|"` |

### Collection Types

#### Complex Object Collections
```csharp
List<OrderItem> items = new()
{
    new OrderItem { ProductId = "A", Quantity = 2 },
    new OrderItem { ProductId = "B", Quantity = 1 }
};
```
- Each item processed **recursively** via `GetStringForSigning`
- Null items → `EmptyValue` marker
- Each item's string terminated with `FieldSeparator`

#### Scalar Collections
```csharp
List<string> tags = new() { "tag1", "tag2", null, "tag3" };
List<int> scores = new() { 10, 20, 30 };
```
- Each element → `ToString()` (null → `EmptyValue`)
- Joined with `FieldSeparator`

#### Array Types
```csharp
string[] names = { "Alice", "Bob" };
int[] numbers = { 1, 2, 3 };
```
- Same handling as collections
- Arrays implement `IEnumerable`

### Nested Objects (Non-Collection)

```csharp
public class Order
{
    public string OrderId { get; set; }
    public Address ShippingAddress { get; set; }  // Complex object
}
```
- **Not recursed** — uses `ToString()` (default object representation)
- To include nested object properties, use a collection or flatten manually

### String Handling

- Strings are **not** treated as collections (special case in code)
- `null` string → `EmptyValue` marker
- Empty string `""` → `""` (not `EmptyValue`)
- Whitespace strings → as-is

## String-for-Signing Format

```
{property1_value}|#FieldSeparator#|{property2_value}|#FieldSeparator#|...|#FieldSeparator#|
```

### Example

```csharp
var obj = new PaymentRequest
{
    MerchantId = "merchant-42",
    Amount = 125.50m,
    Currency = "EUR"
};
```

String-for-signing:
```
merchant-42|#FieldSeparator#|125.50|#FieldSeparator#|EUR|#FieldSeparator#|
```

### With Collections

```csharp
var obj = new Order
{
    OrderId = "ORD-123",
    Items = new List<OrderItem>
    {
        new() { ProductId = "A", Quantity = 2 },
        new() { ProductId = "B", Quantity = 1 }
    },
    Tags = new List<string> { "sale", "new" }
};
```

String-for-signing:
```
ORD-123|#FieldSeparator#|A|#FieldSeparator#|2|#FieldSeparator#|B|#FieldSeparator#|1|#FieldSeparator#|sale|#FieldSeparator#|new|#FieldSeparator#|
```

## Constants

| Constant | Value | Purpose |
|----------|-------|---------|
| `FieldSeparator` | `"|#FieldSeparator#|"` | Separates property values |
| `EmptyValue` | `"|#EmptyValue#|"` | Represents null values |
| `PrefixFormat` | `"|#Length:{0};Checksum:{1}#|"` | Prefix with length + MD5 |
| `StaticSalt` | `"NuciSecurity.HMAC.StaticSalt.8fc5307e-c10b-40d0-b710-de79e7954358"` | Library salt |
| `DefaultOrder` | `int.MaxValue` | Default sort order |

## Type Constraints

- **Generic constraint**: `where TObject : class`
- **Value types**: Not directly supported (must be boxed in a class)
- **Interfaces**: Not supported (no properties to enumerate)
- **Abstract classes**: Supported if they have public properties
- **Anonymous types**: Supported (compiler-generated properties)

## Reflection Details

```csharp
obj.GetType()
    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .Where(p => p.GetCustomAttribute<HmacIgnoreAttribute>() is null)
    .Select(p => new { Property = p, OrderAttr = p.GetCustomAttribute<HmacOrderAttribute>() })
    .OrderBy(x => x.OrderAttr?.Order ?? DefaultOrder)
    .ThenBy(x => x.Property.Name)
    .Select(x => x.Property)
```

### BindingFlags
- `Public`: Only public properties
- `Instance`: Only instance properties (not static)

### Attribute Discovery
- `GetCustomAttribute<HmacIgnoreAttribute>()` — Filter
- `GetCustomAttribute<HmacOrderAttribute>()` — Order extraction

## Test Object Models

### ObjectWithIgnoreAttributes
```csharp
public sealed class ObjectWithIgnoreAttributes
{
    public string UsedProperty { get; set; }

    [HmacIgnore]
    public string IgnoredProperty { get; set; }
}
```

### ObjectWithOrderAttributes
```csharp
public sealed class ObjectWithOrderAttributes
{
    [HmacOrder(1)]
    public string Property1 { get; set; }

    [HmacOrder(2)]
    public string Property2 { get; set; }
}
```

### ObjectWithDifferentOrderAttributes
```csharp
public sealed class ObjectWithDifferentOrderAttributes
{
    [HmacOrder(2)]
    public string Property1 { get; set; }

    [HmacOrder(1)]
    public string Property2 { get; set; }
}
```

### ObjectWithCollectionProperties
```csharp
public sealed class ObjectWithCollectionProperties
{
    public List<ObjectWithIgnoreAttributes> Collection { get; set; }
    public string Text { get; set; }
}
```

## Related

- [Attributes](../components/attributes.md)
- [Token Generation Flow](../flows/token-generation.md)
- [HmacEncoder Component](../components/hmac-encoder.md)