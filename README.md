# JanetSharp

A .NET bridge for embedding the [Janet](https://janet-lang.org/) programming language in C# applications. JanetSharp lets you evaluate Janet code, share data structures, invoke Janet functions from C#, and expose C# callbacks to Janet — all with full GC safety and no risk of runtime crashes from Janet's `longjmp`-based error handling.

## Features

- **Full Janet language** — complete stdlib via two-stage boot (`defn`, `loop`, `map`, `match`, `import`, macros, and all ~500 stdlib functions)
- **Module system** — register Janet modules from C# strings or tables, importable via `(import name)`
- **Safe interop** — C-shim layer catches all Janet panics before they cross P/Invoke boundaries
- **Full type system** — strings, symbols, keywords, arrays, tuples, tables, structs, buffers
- **Bi-directional function calls** — call Janet functions from C#, register C# callbacks callable from Janet
- **Fibers (coroutines)** — create, resume, and yield from Janet fibers with full signal inspection
- **Automatic type coercion** — convert between .NET primitives and Janet values with `JanetConvert`
- **GC-safe smart pointers** — `JanetValue` roots/unroots references in Janet's GC automatically
- **Cross-platform** — native shim builds for Windows, Linux, and macOS via CMake

## Quick Start

```csharp
using JanetSharp;

// Initialize the Janet runtime (one per thread)
using var runtime = new JanetRuntime();

// Evaluate Janet expressions
var result = runtime.Eval("(+ 1 2 3)");
Console.WriteLine(result.AsNumber()); // 6

// Work with Janet types
using var arr = JanetArray.Create();
arr.Add(Janet.From(1.0));
arr.Add(Janet.From(2.0));
arr.Add(Janet.From(3.0));
Console.WriteLine(arr.Count); // 3

// Invoke Janet functions from C#
using var plus = runtime.GetFunction("+");
var sum = plus.Invoke(Janet.From(10.0), Janet.From(20.0));
Console.WriteLine(sum.AsNumber()); // 30

// Register C# callbacks callable from Janet
using var cb = runtime.Register("double-it", args =>
    Janet.From(args[0].AsNumber() * 2));

var doubled = runtime.Eval("(double-it 21)");
Console.WriteLine(doubled.AsNumber()); // 42

// Fibers (coroutines)
using var yieldFn = runtime.GetFunction("(fn [] (yield 1) (yield 2) 3)");
using var fiber = JanetFiber.Create(yieldFn);

Console.WriteLine(fiber.Status);        // New
Console.WriteLine(fiber.Resume());      // Janet(Number) → yielded 1
Console.WriteLine(fiber.Status);        // Pending
Console.WriteLine(fiber.Resume());      // Janet(Number) → yielded 2
Console.WriteLine(fiber.Resume());      // Janet(Number) → returned 3
Console.WriteLine(fiber.CanResume);     // False

// Wrap .NET objects as Janet abstracts (GC-bridged)
using var abs = JanetAbstract.Create(new List<string> { "hello", "world" });
Console.WriteLine(abs.Type);                       // Abstract
var list = abs.GetTarget<List<string>>();           // same reference
Console.WriteLine(list[0]);                        // "hello"

// Modules — register from C#, import from Janet
runtime.Modules.AddModule("mylib", @"
    (defn greet [name] (string ""Hello, "" name ""!""))
");
var greeting = runtime.Eval(@"(import mylib) (mylib/greet ""world"")");
Console.WriteLine(greeting.AsString()); // "Hello, world!"

// Automatic type coercion
Janet j = JanetConvert.ToJanet("hello");
string s = JanetConvert.ToClr<string>(j); // "hello"
```

## Installation

### Prerequisites

- .NET 9.0+ SDK
- CMake 3.15+
- C compiler (Visual Studio 2022 Build Tools on Windows, gcc/clang on Linux/macOS)

### Build from Source

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

> **Note:** A self-contained NuGet package (no manual native build required) is planned for a future release.

## API Overview

### Core Types

| Type | Description |
|------|-------------|
| `JanetRuntime` | Manages the Janet VM lifecycle. One instance per OS thread. |
| `Janet` | 8-byte NaN-boxed value struct. Lightweight, pass by value. |
| `JanetValue` | GC-rooted wrapper (`IDisposable`). Use for heap-allocated Janet values. |
| `JanetFunction` | Wraps a Janet function for safe invocation from C#. |
| `JanetFiber` | Wraps a Janet fiber (coroutine) with resume/yield/status. |
| `JanetAbstract` | Wraps a .NET object as a Janet abstract, GC-bridged. |
| `JanetModule` | Registers modules importable via Janet's `(import name)`. |
| `JanetCallback` | Exposes a C# delegate as a Janet-callable function. |
| `JanetConvert` | Static helper for .NET ↔ Janet type coercion. |

### Collection Types

| Type | Janet Type | C# Interface |
|------|-----------|--------------|
| `JanetString` | string | `JanetValue` + `ToString()`, `AsSpan()` |
| `JanetSymbol` | symbol | `JanetValue` + `ToString()`, `AsSpan()` |
| `JanetKeyword` | keyword | `JanetValue` + `ToString()`, `AsSpan()` |
| `JanetArray` | array | `IList<Janet>` |
| `JanetTuple` | tuple | `IReadOnlyList<Janet>` |
| `JanetTable` | table | `IDictionary<Janet, Janet>` |
| `JanetStruct` | struct | `IReadOnlyDictionary<Janet, Janet>` |
| `JanetBuffer` | buffer | `JanetValue` + `WriteByte()`, `AsSpan()` |
| `JanetAbstract` | abstract | `JanetValue` + `Target`, `GetTarget<T>()` |

## Architecture

JanetSharp uses a **C-shim layer** (`native/janet_shim.c`) between .NET and Janet. This is necessary because Janet uses `setjmp`/`longjmp` for error handling — if a `longjmp` unwinds through a managed P/Invoke frame, the CLR crashes. The shim wraps all Janet API calls so panics are caught by `janet_pcall` and converted to return codes.

Janet values are **64-bit NaN-boxed** unions that cross the P/Invoke boundary as `long`. The `Janet` struct in C# mirrors this layout. Heap-allocated Janet objects (strings, arrays, tables, etc.) must be **GC-rooted** via `janet_gcroot` to prevent Janet's garbage collector from freeing them while C# still holds a reference — `JanetValue` handles this automatically.

C# callbacks are exposed to Janet as **Callable Abstracts**. The C shim registers a custom Janet Abstract type that wraps an unmanaged function pointer. When Janet calls the abstract, a metamethod executes the C# delegate, ensuring a seamless and infinitely scalable callback system.

## Current Limitations

- **Single-threaded (per runtime)** — Janet itself is single-threaded. `JanetRuntime` enforces thread affinity. However, you can safely create multiple `JanetRuntime` instances across different OS threads for true concurrency.
- **macOS support** — the native shim builds on macOS but crashes during `janet_init()` on Apple Silicon CI runners. macOS is temporarily disabled in CI. Local macOS builds may work. <!-- TODO: investigate and re-enable macOS CI -->

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for build instructions, architecture overview, and development guidelines.

## Documentation

See the [User Guide](docs/README.md) for detailed guides on types, function calls, callbacks, type coercion, and memory management.

## Roadmap

See [ROADMAP.md](ROADMAP.md) for the full development plan.

## License

[MIT](LICENSE)
