# Contributing to JanetSharp

## Prerequisites

- [.NET 9.0+ SDK](https://dotnet.microsoft.com/download)
- [CMake 3.15+](https://cmake.org/download/)
- C compiler:
  - **Windows**: Visual Studio 2022 Build Tools (MSVC)
  - **Linux**: gcc or clang
  - **macOS**: Xcode command-line tools (clang)

## Building

### 1. Clone with submodules

```bash
git clone --recurse-submodules https://github.com/andreamancuso/Janet-CSharp.git
cd Janet-CSharp
```

If you already cloned without `--recurse-submodules`:

```bash
git submodule update --init --recursive
```

### 2. Build the native shim

```bash
cd native
cmake -B build
cmake --build build --config Release
cd ..
```

This produces `native/build/Release/janet_shim.dll` (Windows), `.so` (Linux), or `.dylib` (macOS). The `.csproj` automatically copies it to the output directory.

### 3. Build and test

```bash
dotnet build
dotnet test
```

## Architecture Overview

### C-Shim Layer

Janet uses `setjmp`/`longjmp` for error handling. If a `longjmp` unwinds through a C# P/Invoke frame, the CLR crashes. The C-shim (`native/janet_shim.c`) wraps all Janet API calls so that panics are caught by `janet_pcall` internally and converted to return codes that C# can handle safely.

**Rule: Never call a Janet API function directly from C#.** Always go through the shim.

### Value Marshalling

Janet values are 64-bit NaN-boxed unions. They cross the P/Invoke boundary as `long` (8 bytes). The `Janet` struct in C# uses `LayoutKind.Explicit` to match this layout. Pointer types (tables, functions, fibers) are passed as `IntPtr`.

### GC Rooting

Janet has its own garbage collector. When C# holds a reference to a heap-allocated Janet object (string, array, table, etc.), it must be **rooted** with `janet_gcroot()` to prevent collection. The `JanetValue` base class handles this automatically — it roots on creation and unroots on `Dispose()`. Primitive types (number, nil, boolean) skip rooting since they don't participate in Janet's GC.

### Callback Trampolines

C# callbacks are exposed as native Callable Abstracts implementing the `call` metamethod. This avoids the limitations of static trampoline tables and allows an infinite number of C# delegates to be registered safely, catching `longjmp` panics in the C-shim before they reach managed frames.

### Build Pipeline (Two-Stage Boot)

The native build uses a two-stage process to embed the full Janet stdlib:

1. **Stage 1** — CMake compiles `janet_boot` (an executable) from Janet's core C files + boot compiler, with `JANET_BOOTSTRAP` defined.
2. **Stage 2** — `janet_boot` runs `boot.janet` to produce `janet_core_image.c`, a serialized binary image of the full Janet stdlib.
3. **Stage 3** — CMake compiles `janet_shim.dll` from the core C files + the generated image + `janet_shim.c`, **without** `JANET_BOOTSTRAP`. The full Janet language is available at runtime.

This all happens automatically via `cmake --build`. Contributors don't need to run any extra steps.

## Adding a New Shim Function

End-to-end process for exposing a new Janet API to C#:

### 1. Add the C function to `native/janet_shim.c`

```c
SHIM_API int32_t shim_example_function(int64_t value, int64_t* out) {
    Janet j = *(Janet*)&value;
    // ... call Janet APIs ...
    *out = *(int64_t*)&result;
    return 0; // success
}
```

- Use `SHIM_API` for export visibility
- Accept/return Janet values as `int64_t`
- Wrap any panicking calls in `janet_pcall` if needed

### 2. Add the P/Invoke declaration to `NativeMethods.cs`

```csharp
[LibraryImport(LibName)]
internal static partial int shim_example_function(long value, out long result);
```

- Use `LibraryImport` (source-generated), not `DllImport`
- Match the C types: `int64_t` → `long`, `int32_t` → `int`, `void*` → `IntPtr`

### 3. Add the C# wrapper

Create or extend a class in `src/JanetSharp.Core/`:

```csharp
public Janet ExampleMethod()
{
    int status = NativeMethods.shim_example_function(Value.RawValue, out long result);
    if (status != 0) throw new JanetException("...");
    return new Janet(result);
}
```

### 4. Add tests

Add test cases to the appropriate file in `tests/JanetSharp.Tests/`:

```csharp
[Fact]
public void ExampleMethod_Works()
{
    var result = /* ... */;
    Assert.Equal(expected, result);
}
```

## Code Conventions

- **P/Invoke**: Use `LibraryImport` (source-generated, .NET 7+), not `DllImport`
- **Value types**: Use `Janet` struct for transient values; `JanetValue` for GC-rooted heap references
- **Testing**: xUnit, one test class per feature area
- **Unsafe code**: `AllowUnsafeBlocks` is enabled for span/pointer operations — use `unsafe` only when needed for performance-critical marshalling
- **Target framework**: `net9.0`

## Testing

Tests are organized by feature area:

- `RuntimeTests.cs` — runtime lifecycle, init/dispose, eval
- `ValueTests.cs` — Janet struct and JanetValue smart pointer
- `StringTests.cs` — strings, symbols, keywords
- `CollectionTests.cs` — arrays, tuples, buffers
- `TableTests.cs` — tables and structs (immutable maps)
- `FunctionTests.cs` — function invocation, callbacks, type coercion
- `FiberTests.cs` — fiber creation, resume, yield, signals
- `AbstractTests.cs` — .NET object bridging via JanetAbstract
- `StdlibTests.cs` — full Janet stdlib (macros, iteration, pattern matching, etc.)
- `ModuleTests.cs` — module registration and import
- `SafetyTests.cs` — dispose safety, type errors, thread affinity, edge cases
- `StressTests.cs` — GC pressure, hot loops, large data churn

Run all tests:

```bash
dotnet test
```

Run a specific test class:

```bash
dotnet test --filter "FullyQualifiedName~JanetFunctionTests"
```
