# Type Coercion

`JanetConvert` provides automatic conversion between .NET types and Janet values, useful when working with dynamically-typed data.

## .NET to Janet: `ToJanet`

```csharp
Janet a = JanetConvert.ToJanet(42.0);       // Janet Number
Janet b = JanetConvert.ToJanet(7);           // Janet Number (int → double)
Janet c = JanetConvert.ToJanet(true);        // Janet Boolean
Janet d = JanetConvert.ToJanet("hello");     // Janet String
Janet e = JanetConvert.ToJanet(null);        // Janet Nil
Janet f = JanetConvert.ToJanet(3.14f);       // Janet Number (float → double)
Janet g = JanetConvert.ToJanet(100L);        // Janet Number (long → double)
```

If you pass a `Janet` value, it is returned as-is:

```csharp
Janet original = Janet.From(42.0);
Janet same = JanetConvert.ToJanet(original); // no conversion needed
```

Unsupported types throw `ArgumentException`:

```csharp
JanetConvert.ToJanet(DateTime.Now); // ArgumentException
```

## Janet to .NET: `ToClr<T>`

```csharp
Janet num = Janet.From(42.0);
double d = JanetConvert.ToClr<double>(num);  // 42.0
int i = JanetConvert.ToClr<int>(num);        // 42
float f = JanetConvert.ToClr<float>(num);    // 42.0f
long l = JanetConvert.ToClr<long>(num);      // 42L

Janet flag = Janet.From(true);
bool b = JanetConvert.ToClr<bool>(flag);     // true

Janet str = Janet.From("hello");
string s = JanetConvert.ToClr<string>(str);  // "hello"
```

You can also get the raw `Janet` value back:

```csharp
Janet val = JanetConvert.ToClr<Janet>(num);  // returns the Janet as-is
```

## Nil Handling

Janet `nil` converts to `null` for reference types and nullable value types:

```csharp
Janet nil = Janet.Nil;
string? s = JanetConvert.ToClr<string?>(nil); // null
```

Converting `nil` to a non-nullable value type throws `InvalidOperationException`:

```csharp
JanetConvert.ToClr<double>(Janet.Nil); // InvalidOperationException
JanetConvert.ToClr<int>(Janet.Nil);    // InvalidOperationException
```

## Non-Generic Variant

`ToClr(Janet, Type)` accepts the target type at runtime:

```csharp
object? value = JanetConvert.ToClr(Janet.From(42.0), typeof(double)); // 42.0 (boxed)
object? name = JanetConvert.ToClr(Janet.From("Alice"), typeof(string)); // "Alice"
```

## Supported Type Mappings

| .NET Type | Janet Type | Notes |
|-----------|-----------|-------|
| `double` | Number | Direct mapping |
| `int` | Number | Truncated to integer |
| `float` | Number | Narrowed from double |
| `long` | Number | Narrowed from double |
| `bool` | Boolean | Direct mapping |
| `string` | String | UTF-8 encoded/decoded |
| `null` | Nil | Reference types only |
| `Janet` | Any | Pass-through |

## When to Use JanetConvert vs Janet.From

`Janet.From()` and implicit conversions are best when you know the type at compile time:

```csharp
Janet x = Janet.From(42.0);  // preferred — zero overhead
Janet y = 42.0;              // also fine — implicit conversion
```

### Implicit Conversions for JanetValue Wrappers

Any subclass of `JanetValue` (like `JanetArray`, `JanetTable`, `JanetFunction`, etc.) can be implicitly converted to the raw `Janet` struct. This makes passing your GC-rooted wrappers into functions seamless:

```csharp
using var table = JanetTable.Create();
table["key"] = Janet.From("value");

// Implicit conversion from JanetTable to Janet struct
runtime.GetFunction("process-data").Invoke(table); 
```

`JanetConvert.ToJanet()` is useful when the type is only known at runtime (e.g., from reflection or generic code):

```csharp
object dynamicValue = GetValueFromSomewhere();
Janet j = JanetConvert.ToJanet(dynamicValue);
```
