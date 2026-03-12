# Working with Types

JanetSharp maps Janet's type system to C# types. The `Janet` struct is a lightweight 8-byte value that can hold any Janet type. For heap-allocated types (strings, arrays, tables, etc.), wrapper classes provide GC-safe access with familiar .NET interfaces.

## Primitives

Primitives are encoded directly in the 64-bit NaN-box and don't need GC rooting.

```csharp
// Numbers (64-bit float)
Janet num = Janet.From(42.0);
Janet intNum = Janet.From(7);       // stored as double
double d = num.AsNumber();          // 42.0
int i = intNum.AsInteger();         // 7

// Booleans
Janet t = Janet.True;
Janet f = Janet.From(false);
bool b = t.AsBoolean();            // true

// Nil
Janet nil = Janet.Nil;
bool isNil = nil.IsNil;            // true

// Implicit conversions
Janet x = 3.14;                    // double -> Janet
Janet y = 42;                      // int -> Janet
Janet z = true;                    // bool -> Janet
Janet s = "hello";                 // string -> Janet
```

## Type Inspection

```csharp
Janet val = Janet.From(42.0);
JanetType type = val.Type;         // JanetType.Number
bool truthy = val.IsTruthy;        // true (everything except nil and false)
```

## Strings

Janet strings are immutable UTF-8 byte sequences.

```csharp
// Create from .NET string
using var js = JanetString.Create("hello world");

// Access
string text = js.ToString();              // "hello world"
int len = js.Length;                       // 11 (byte length)
ReadOnlySpan<byte> raw = js.AsSpan();     // zero-copy UTF-8 access

// From eval
var result = runtime.Eval("\"hello\"");
string s = result.AsString();             // works for String, Symbol, Keyword
```

## Symbols and Keywords

Symbols are interned identifiers. Keywords are interned identifiers prefixed with `:` in Janet.

```csharp
using var sym = JanetSymbol.Create("my-var");
using var kw = JanetKeyword.Create("name");    // :name in Janet

string symName = sym.ToString();    // "my-var"
string kwName = kw.ToString();     // "name"
```

## Arrays

Mutable, ordered collections. Implements `IList<Janet>`.

```csharp
using var arr = JanetArray.Create();

// Add elements
arr.Add(Janet.From(1.0));
arr.Add(Janet.From(2.0));
arr.Add(Janet.From(3.0));

// Access
int count = arr.Count;                    // 3
Janet first = arr[0];                     // Janet(Number) = 1.0
arr[1] = Janet.From(20.0);               // set by index

// Stack operations
Janet last = arr.Pop();                   // removes and returns last element

// Search
bool has = arr.Contains(Janet.From(1.0)); // true
int idx = arr.IndexOf(Janet.From(1.0));   // 0

// Iterate
foreach (var item in arr)
    Console.WriteLine(item.AsNumber());
```

## Tuples

Immutable, ordered collections. Implements `IReadOnlyList<Janet>`.

```csharp
// Create from values
using var tup = JanetTuple.Create(
    Janet.From(1.0),
    Janet.From(2.0),
    Janet.From(3.0));

// Access (read-only)
int count = tup.Count;     // 3
Janet first = tup[0];      // 1.0

// Iterate
foreach (var item in tup)
    Console.WriteLine(item.AsNumber());
```

## Tables

Mutable hash maps. Implements `IDictionary<Janet, Janet>`.

```csharp
using var tbl = JanetTable.Create();

// Add entries
tbl[Janet.From("name")] = Janet.From("Alice");
tbl[Janet.From("age")] = Janet.From(30.0);

// Lookup
Janet name = tbl[Janet.From("name")];           // "Alice"
bool found = tbl.TryGetValue(Janet.From("age"), out var age);
bool has = tbl.ContainsKey(Janet.From("name"));  // true

// Remove
tbl.Remove(Janet.From("age"));

// Clear
tbl.Clear();

// Count
int count = tbl.Count;
```

> **Note:** Table enumeration (Keys, Values, GetEnumerator) is not yet supported — it requires iteration support in the native shim (planned for a future phase).

## Structs

Immutable hash maps. Implements `IReadOnlyDictionary<Janet, Janet>`. Created via Janet `eval` since Janet structs require a special construction process.

```csharp
var result = runtime.Eval("(struct :x 1 :y 2)");
using var st = result.AsStruct();

Janet x = st[Janet.From("x")];                  // 1.0
bool has = st.ContainsKey(Janet.From("y"));      // true
int count = st.Count;                            // 2
```

> **Note:** Struct enumeration is not yet supported (same as tables).

## Buffers

Mutable byte sequences. Useful for binary data.

```csharp
using var buf = JanetBuffer.Create(64);

// Write data
buf.WriteByte(0x48);                              // 'H'
buf.WriteBytes(new byte[] { 0x65, 0x6C, 0x6C, 0x6F }); // "ello"

// Read data
int len = buf.Count;                              // 5
ReadOnlySpan<byte> data = buf.AsSpan();           // zero-copy access

// Resize
buf.SetCount(3);                                  // truncate to 3 bytes
buf.EnsureCapacity(1024);                         // grow backing storage
```
