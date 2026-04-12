# Getting Started

## Prerequisites

- [.NET 9.0+ SDK](https://dotnet.microsoft.com/download)
- [CMake 3.15+](https://cmake.org/download/)
- C compiler: Visual Studio 2022 Build Tools (Windows), gcc (Linux), or clang (macOS)

## Build from Source

```bash
# Clone with submodules
git clone --recurse-submodules https://github.com/andreamancuso/Janet-CSharp.git
cd Janet-CSharp

# Build the native shim
cd native
cmake -B build
cmake --build build --config Release
cd ..

# Build and test
dotnet build
dotnet test
```

## Your First Script

```csharp
using JanetSharp;

// Initialize the Janet runtime (one per thread)
using var runtime = new JanetRuntime();

// Evaluate a Janet expression
var result = runtime.Eval("(+ 1 2 3)");
Console.WriteLine(result.AsNumber()); // 6
```

Key points:
- `JanetRuntime` must be disposed when done (use `using`)
- Only one `JanetRuntime` can exist per OS thread (Janet is thread-local)
- All runtime access must happen on the thread that created it

## Invoking Janet Functions

```csharp
using var runtime = new JanetRuntime();

// Get a built-in function
using var plus = runtime.GetFunction("+");

// Call it with arguments
var sum = plus.Invoke(Janet.From(10.0), Janet.From(20.0));
Console.WriteLine(sum.AsNumber()); // 30
```

## Registering C# Callbacks

```csharp
using var runtime = new JanetRuntime();

// Register a C# function callable from Janet
using var cb = runtime.Register("double-it", args =>
    Janet.From(args[0].AsNumber() * 2));

// Call it from Janet code
var result = runtime.Eval("(double-it 21)");
Console.WriteLine(result.AsNumber()); // 42
```

## Error Handling

```csharp
using var runtime = new JanetRuntime();

// Throwing variant — throws JanetException on error
try
{
    runtime.Eval("(error \"something went wrong\")");
}
catch (JanetException ex)
{
    Console.WriteLine(ex.Signal);     // Error
    Console.WriteLine(ex.ErrorValue); // Janet(String)
}

// Non-throwing variant — inspect the signal yourself
var result = runtime.Eval("(error \"oops\")", out var signal);
if (signal != JanetSignal.Ok)
{
    Console.WriteLine($"Error: {result.AsString()}");
}
```

## Next Steps

- [Working with Types](types.md) — strings, arrays, tables, and more
- [Calling Janet from C#](calling-janet.md) — function invocation patterns
- [Calling C# from Janet](calling-csharp.md) — callback registration
- [Type Coercion](type-coercion.md) — automatic .NET/Janet conversion
- [Memory Management](memory-management.md) — GC rooting and disposal
