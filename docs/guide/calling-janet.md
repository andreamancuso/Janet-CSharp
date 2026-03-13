# Calling Janet from C#

## Evaluating Expressions

The simplest way to run Janet code is `Eval`:

```csharp
using var runtime = new JanetRuntime();

// Throwing variant — throws JanetException on error
Janet result = runtime.Eval("(+ 1 2 3)");
double value = result.AsNumber(); // 6.0

// Non-throwing variant — returns the signal for inspection
Janet result2 = runtime.Eval("(error \"oops\")", out JanetSignal signal);
if (signal == JanetSignal.Error)
{
    Console.WriteLine($"Error: {result2.AsString()}");
}
```

## Invoking Functions

For repeated calls, get a function reference and invoke it directly:

```csharp
// Get a built-in function
using var plus = runtime.GetFunction("+");

// Invoke with arguments
Janet sum = plus.Invoke(Janet.From(10.0), Janet.From(20.0));
Console.WriteLine(sum.AsNumber()); // 30.0

// Multiple arguments
Janet total = plus.Invoke(
    Janet.From(1.0),
    Janet.From(2.0),
    Janet.From(3.0),
    Janet.From(4.0));
Console.WriteLine(total.AsNumber()); // 10.0
```

## Error Handling

### Throwing Variant

`Invoke(params Janet[])` throws `JanetException` when the function signals an error:

```csharp
using var errorFn = runtime.GetFunction("error");

try
{
    errorFn.Invoke(Janet.From("something failed"));
}
catch (JanetException ex)
{
    Console.WriteLine(ex.Signal);     // JanetSignal.Error
    Console.WriteLine(ex.ErrorValue); // Janet(String)
}
```

### Non-Throwing Variant

`Invoke(Janet[], out JanetSignal)` returns the signal without throwing:

```csharp
using var div = runtime.GetFunction("/");

var args = new[] { Janet.From(10.0), Janet.From(0.0) };
Janet result = div.Invoke(args, out JanetSignal signal);

if (signal != JanetSignal.Ok)
{
    Console.WriteLine("Division failed");
}
```

## Signal Codes

| Signal | Value | Meaning |
|--------|-------|---------|
| `Ok` | 0 | Successful completion |
| `Error` | 1 | An error occurred |
| `Debug` | 2 | A debug breakpoint was hit |
| `Yield` | 3 | A fiber yielded a value |

## Defining Functions in Janet

You can define functions in Janet using `fn` and then invoke them from C#:

```csharp
// Define a function in Janet and get a reference
using var square = runtime.GetFunction("(fn [x] (* x x))");

Janet result = square.Invoke(Janet.From(7.0));
Console.WriteLine(result.AsNumber()); // 49.0
```

## Available Functions

JanetSharp includes the **complete Janet standard library** (~500 functions and macros):

- Arithmetic: `+`, `-`, `*`, `/`, `%`, `mod`
- Comparison: `<`, `>`, `<=`, `>=`, `=`, `not=`
- Logic: `not`, `and`, `or`
- Type: `type`, `length`
- I/O: `print`, `prin`, `pp`
- Control: `if`, `do`, `fn`, `error`, `when`, `unless`, `cond`, `case`
- Data: `array`, `table`, `struct`, `tuple`, `buffer`, `string`
- Macros: `defn`, `defmacro`, `let`, `if-let`, `match`
- Iteration: `loop`, `for`, `each`, `map`, `filter`, `reduce`, `keep`, `find`
- Strings: `string/format`, `string/join`, `string/split`
- Modules: `import`, `require`, `use`

## Modules

You can register Janet modules from C# and import them from Janet code:

```csharp
// Register a module from source
runtime.Modules.AddModule("mathlib", @"
    (def pi 3.14159)
    (defn circle-area [r] (* pi r r))
");

// Import and use from Janet
var area = runtime.Eval(@"
    (import mathlib)
    (mathlib/circle-area 5)
");

// Modules can depend on other modules
runtime.Modules.AddModule("shapes", @"
    (import mathlib)
    (defn sphere-volume [r] (* (/ 4 3) mathlib/pi r r r))
");
```

Module names must be simple identifiers (no `/`, `.`, or `@` prefix).
