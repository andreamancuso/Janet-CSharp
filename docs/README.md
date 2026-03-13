# JanetSharp Documentation

## User Guide

- [Getting Started](guide/getting-started.md) — prerequisites, build, first script
- [Working with Types](guide/types.md) — primitives, strings, arrays, tuples, tables, structs, buffers
- [Calling Janet from C#](guide/calling-janet.md) — eval, function invocation, error handling
- [Calling C# from Janet](guide/calling-csharp.md) — callbacks, exception safety, lifetime management
- [Type Coercion](guide/type-coercion.md) — automatic .NET/Janet type conversion
- [Memory Management](guide/memory-management.md) — GC rooting, disposal patterns, common pitfalls

## API Reference

All public types and members have XML doc comments. Use IntelliSense in your IDE or browse the source in [`src/JanetSharp.Core/`](../src/JanetSharp.Core/).

### Key Types

| Type | Description |
|------|-------------|
| `JanetRuntime` | Manages the Janet VM lifecycle |
| `Janet` | 64-bit NaN-boxed value (the core value type) |
| `JanetValue` | GC-safe handle that roots Janet values |
| `JanetFunction` | Invoke Janet functions from C# |
| `JanetFiber` | Create and resume Janet fibers (coroutines) |
| `JanetCallback` | Expose C# functions to Janet |
| `JanetConvert` | Automatic type conversion |
| `JanetString` / `JanetSymbol` / `JanetKeyword` | Immutable text types |
| `JanetArray` / `JanetTuple` | Ordered collections (mutable / immutable) |
| `JanetTable` / `JanetStruct` | Key-value maps (mutable / immutable) |
| `JanetBuffer` | Mutable byte sequence |
| `JanetException` | Error propagation from Janet to C# |
