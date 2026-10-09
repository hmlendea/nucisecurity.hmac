# Attributes Component

**Files**:
- `NuciSecurity.HMAC/HmacIgnoreAttribute.cs`
- `NuciSecurity.HMAC/HmacOrderAttribute.cs`

**Purpose**: Declarative configuration for token generation behavior

---

## HmacIgnoreAttribute

**File**: `NuciSecurity.HMAC/HmacIgnoreAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class HmacIgnoreAttribute : Attribute { }
```

### Purpose

Marks a property to be **excluded** from HMAC token generation.

### Usage

```csharp
public class PaymentRequest
{
    public string MerchantId { get; set; }
    public decimal Amount { get; set; }

    [HmacIgnore]
    public string InternalNotes { get; set; }  // Excluded from token

    [HmacIgnore]
    public DateTime CreatedAt { get; set; }    // Excluded from token
}
```

### Behavior

- **Target**: Properties only (`AttributeTargets.Property`)
- **Effect**: Property completely omitted from string-for-signing
- **No ordering impact**: Ignored properties don't affect order of other properties
- **Inheritance**: Not inherited (applied per-property)

### Implementation

In `HmacEncoder.GetStringForSigning`:

```csharp
.Where(p => p.GetCustomAttribute<HmacIgnoreAttribute>() is null)
```

Properties with this attribute are filtered out **before** sorting.

### Test Coverage

`HmacEncoderTests.GivenAnObjectWithIgnoreAttributes_WhenGeneratingTheHmacToken_ThenTheExpectedValueIsReturned`:
- Two test cases with different ignored values producing same token
- Confirms ignored property value doesn't affect token

---

## HmacOrderAttribute

**File**: `NuciSecurity.HMAC/HmacOrderAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class HmacOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
```

### Purpose

Specifies the **processing order** of a property during token generation.

### Usage

```csharp
public class PaymentRequest
{
    [HmacOrder(1)]
    public string MerchantId { get; set; }

    [HmacOrder(2)]
    public decimal Amount { get; set; }

    [HmacOrder(3)]
    public string Currency { get; set; }

    public string ExtraField { get; set; }  // No order → processed after ordered properties
}
```

### Constructor

| Parameter | Type | Description |
|-----------|------|-------------|
| `order` | `int` | Order value. Lower values processed first. |

### Property

| Name | Type | Description |
|------|------|-------------|
| `Order` | `int` | The order value (read-only) |

### Behavior

- **Target**: Properties only (`AttributeTargets.Property`)
- **Sorting**: Properties with `[HmacOrder]` processed first, by `Order` ascending
- **Tie-breaking**: Same `Order` value → alphabetical by property name
- **Unordered properties**: Processed after all ordered properties, alphabetical by name
- **Default order**: `int.MaxValue` (internal constant `DefaultOrder`)

### Sorting Algorithm

In `HmacEncoder.GetStringForSigning`:

```csharp
.Select(p => new { Property = p, OrderAttr = p.GetCustomAttribute<HmacOrderAttribute>() })
.OrderBy(x => x.OrderAttr?.Order ?? DefaultOrder)
.ThenBy(x => x.Property.Name)
.Select(x => x.Property)
```

### Example: Order Effect

```csharp
// ObjectWithOrderAttributes: Property1(1), Property2(2)
// ObjectWithDifferentOrderAttributes: Property1(2), Property2(1)

var obj1 = new ObjectWithOrderAttributes { Property1 = "a", Property2 = "b" };
var obj2 = new ObjectWithDifferentOrderAttributes { Property1 = "a", Property2 = "b" };

string token1 = HmacEncoder.GenerateToken(obj1, secret);
string token2 = HmacEncoder.GenerateToken(obj2, secret);

// token1 != token2 (different property order)
```

### Test Coverage

`HmacEncoderTests.GivenAnObjectWithOrderAttributes_WhenGeneratingTheHmacToken_ThenTheTokenWillDifferIfTheOrderIsChanged`:
- Two objects with same property values but different order attributes
- Confirms tokens differ when order changes

---

## Combined Usage

```csharp
public class ComplexDto
{
    [HmacOrder(1)]
    public string Id { get; set; }

    [HmacOrder(2)]
    public string Name { get; set; }

    [HmacIgnore]
    public string InternalId { get; set; }

    [HmacOrder(3)]
    public List<ChildDto> Children { get; set; }

    public string UnorderedField { get; set; }  // Processed after ordered, alphabetically
}
```

**Processing order**:
1. `Id` (Order 1)
2. `Name` (Order 2)
3. `Children` (Order 3)
4. `UnorderedField` (no order, alphabetical)
5. `InternalId` → **EXCLUDED** (ignored)

---

## Attribute Interaction

| Scenario | Result |
|----------|--------|
| Both `[HmacIgnore]` and `[HmacOrder]` on same property | `[HmacIgnore]` wins — property excluded entirely |
| `[HmacOrder]` on ignored property | Order value irrelevant (property filtered first) |
| Duplicate `Order` values | Alphabetical by property name |
| Negative `Order` values | Valid, processed before positive values |
| `Order` = `int.MaxValue` | Same as no attribute (processed last) |

---

## Reflection Details

- **Discovery**: `PropertyInfo.GetCustomAttribute<T>()`
- **Binding**: `BindingFlags.Public | BindingFlags.Instance`
- **Inheritance**: Attributes not inherited from base classes (per-property)
- **Static properties**: Excluded (Instance flag only)
- **Private properties**: Excluded (Public flag only)

---

## Modification Impact

| Change | Impact |
|--------|--------|
| Add `[HmacIgnore]` to existing property | **Breaking** — token changes for all existing objects |
| Remove `[HmacIgnore]` | **Breaking** — token changes |
| Change `[HmacOrder]` value | **Breaking** — token changes if order changes |
| Add `[HmacOrder]` to unordered property | **Breaking** — token changes (moves property position) |
| Change attribute targets | **Breaking** — may affect different properties |

**Versioning**: Any attribute change on a DTO used for HMAC is a breaking change for token compatibility.