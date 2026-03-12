# JanetSharp

A high-performance .NET bridge for embedding the [Janet](https://janet-lang.org/) programming language in C# applications. JanetSharp lets you evaluate Janet code, share data structures, invoke Janet functions from C#, and expose C# callbacks to Janet — all with full GC safety and no risk of runtime crashes from Janet's `longjmp`-based error handling.

## Features

- **Safe interop** — C-shim layer catches all Janet panics before they cross P/Invoke boundaries
- **Full type system** — strings, symbols, keywords, arrays, tuples, tables, structs, buffers
- **Bi-directional function calls** — call Janet functions from C#, register C# callbacks callable from Janet
- **Automatic type coercion** — convert between .NET primitives and Janet values with `JanetConvert`
- **GC-safe smart pointers** — `JanetValue` roots/unroots references in Janet's GC automatically
- **Cross-platform** — native shim builds for Windows, Linux, and macOS via CMake

## Quick Start

```csharp
using JanetSharp;

// Initialize the Janet runtime (one per process)
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
| `JanetRuntime` | Manages the Janet VM lifecycle. One instance per process. |
| `Janet` | 8-byte NaN-boxed value struct. Lightweight, pass by value. |
| `JanetValue` | GC-rooted wrapper (`IDisposable`). Use for heap-allocated Janet values. |
| `JanetFunction` | Wraps a Janet function for safe invocation from C#. |
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

## Architecture

JanetSharp uses a **C-shim layer** (`native/janet_shim.c`) between .NET and Janet. This is necessary because Janet uses `setjmp`/`longjmp` for error handling — if a `longjmp` unwinds through a managed P/Invoke frame, the CLR crashes. The shim wraps all Janet API calls so panics are caught by `janet_pcall` and converted to return codes.

Janet values are **64-bit NaN-boxed** unions that cross the P/Invoke boundary as `long`. The `Janet` struct in C# mirrors this layout. Heap-allocated Janet objects (strings, arrays, tables, etc.) must be **GC-rooted** via `janet_gcroot` to prevent Janet's garbage collector from freeing them while C# still holds a reference — `JanetValue` handles this automatically.

C# callbacks use a **64-slot trampoline system** in the C shim. Each slot has a pre-compiled C function that dispatches to a registered managed delegate, ensuring `longjmp` never passes through managed frames.

## Current Limitations

- **JANET_BOOTSTRAP mode** — only C-level built-in functions are available (`+`, `-`, `*`, `/`, `print`, `type`, `length`, `error`, `fn`, etc.). Janet standard library functions (`defn`, `loop`, `map`, `filter`, etc.) are not available.
- **Table/struct enumeration** — iterating over table/struct entries is not yet implemented.
- **Single-threaded** — Janet is inherently single-threaded. `JanetRuntime` enforces thread affinity.
- **64 callback slots** — maximum of 64 concurrent C# callbacks registered with Janet.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for build instructions, architecture overview, and development guidelines.

## Roadmap

See [ROADMAP.md](ROADMAP.md) for the full development plan.

## License

[MIT](LICENSE)
