# Memory Management

JanetSharp bridges two garbage collectors — .NET's and Janet's. Understanding how they interact prevents leaks and crashes.

## How It Works

Janet uses a tracing garbage collector. When a Janet value (string, array, table, function, etc.) has no references from Janet's root set, it gets collected. But C# code may still hold a reference — if Janet collects it, the C# side gets a dangling pointer.

`JanetValue` solves this by **rooting** values in Janet's GC when created and **unrooting** them when disposed:

```csharp
// JanetValue roots the array in Janet's GC
using var arr = JanetArray.Create();

// The array is safe from Janet GC as long as 'arr' is alive
arr.Add(Janet.From(1.0));

// Dispose unroots it — Janet GC can now collect it
```

## What Needs Rooting

**Primitives do NOT need rooting** — they are encoded entirely within the 64-bit NaN-box:
- `Janet.From(42.0)` — number, no allocation
- `Janet.From(true)` — boolean, no allocation
- `Janet.Nil` — nil, no allocation

**Reference types DO need rooting** — they point to Janet heap memory:
- `JanetString`, `JanetSymbol`, `JanetKeyword`
- `JanetArray`, `JanetTuple`
- `JanetTable`, `JanetStruct`
- `JanetBuffer`
- `JanetFunction`, `JanetCallback`
- `JanetAbstract`

All `JanetValue` subclasses handle rooting automatically. You just need to dispose them.

## The Dispose Pattern

Always use `using` to ensure timely disposal:

```csharp
using var runtime = new JanetRuntime();

// Good: 'using' ensures disposal
using var arr = JanetArray.Create();
using var tbl = JanetTable.Create();
using var fn = runtime.GetFunction("+");

// Also good: explicit dispose
var buf = JanetBuffer.Create(64);
// ... use buf ...
buf.Dispose();
```

### What Happens If You Don't Dispose

If a `JanetValue` is not explicitly disposed, its finalizer will unroot it when the .NET GC collects it. This works but has caveats:

1. **Non-deterministic timing** — the value stays rooted until the .NET GC runs, consuming Janet memory longer than necessary.
2. **Deferred Unrooting** — Finalizers run on the .NET GC thread, but Janet is strictly single-threaded. To avoid crashing the Janet VM, the finalizer pushes the pointer to a thread-safe queue. The creating `JanetRuntime` processes this queue automatically before evaluating new scripts or during its own disposal.

## Cross-Thread Garbage Collection

Because `JanetRuntime` supports multiple thread-local instances, finalizers must handle unrooting safely across threads.

```
Runtime A created on Thread 1
  → JanetValue X created, records reference to Runtime A
Runtime A disposed (calls janet_deinit — all Janet memory freed)
  → .NET GC runs, finalizes JanetValue X
  → X checks: Runtime A is disposed? Yes → skips unroot (safe!)
```

If Runtime A is *still active* when the finalizer runs, calling `janet_gcunroot` directly from the .NET GC thread would crash the single-threaded Janet VM. Instead, the finalizer queues the unrooting operation, and Runtime A processes the queue safely on Thread 1 before its next evaluation.

## Common Pitfalls

### 1. Disposing Too Early

```csharp
// WRONG: callback disposed before Janet can call it
var cb = runtime.Register("my-func", args => Janet.Nil);
cb.Dispose();
runtime.Eval("(my-func)"); // undefined behavior!

// RIGHT: keep it alive
using var cb2 = runtime.Register("my-func", args => Janet.Nil);
runtime.Eval("(my-func)"); // works
```

### 2. Using After Dispose

```csharp
var arr = JanetArray.Create();
arr.Dispose();
arr.Add(Janet.From(1.0)); // throws ObjectDisposedException
```

### 3. Accessing Raw Janet Values After Runtime Disposal

Raw `Janet` values (the struct, not `JanetValue`) are just 64-bit numbers. They're safe to hold but meaningless after the runtime is disposed — the memory they point to is freed:

```csharp
Janet raw;
using (var runtime = new JanetRuntime())
{
    raw = runtime.Eval("\"hello\"");
    // raw.AsString() works here
}
// raw.AsString() — undefined behavior! The string memory is freed.
```

### 4. Thread Safety

Janet itself is single-threaded. All runtime access for a specific `JanetRuntime` must happen on the thread that created it. However, you can safely create multiple independent `JanetRuntime` instances across different OS threads for concurrent execution:

```csharp
// Safe: Multiple runtimes on different threads
Task.Run(() => {
    using var runtime1 = new JanetRuntime();
    runtime1.Eval("(+ 1 2)");
});

Task.Run(() => {
    using var runtime2 = new JanetRuntime();
    runtime2.Eval("(+ 3 4)");
});

using var runtime = new JanetRuntime();

// WRONG: accessing a runtime from another thread
Task.Run(() => runtime.Eval("(+ 1 2)")); // throws InvalidOperationException

// RIGHT: stay on the creating thread
var result = runtime.Eval("(+ 1 2)");
```

## Runtime Disposal Order

When disposing the runtime, JanetSharp flushes pending finalizers before shutting down Janet:

```
JanetRuntime.Dispose()
  → GC.Collect()                    // trigger finalizers for unreachable JanetValues
  → GC.WaitForPendingFinalizers()   // wait for unroot calls to complete
  → shim_deinit()                   // now safe to shut down Janet
```

This ensures that any `JanetValue` objects that went out of scope but weren't explicitly disposed still get properly unrooted before `janet_deinit` frees all Janet memory.

## Summary

| Rule | Why |
|------|-----|
| Always dispose `JanetValue` objects | Frees Janet GC roots promptly |
| Use `using` declarations | Guarantees disposal even on exceptions |
| Keep callbacks alive while in use | Janet holds a raw function pointer |
| Dispose `JanetAbstract` when done | Frees the GCHandle bridging .NET and Janet GC |
| Don't use raw `Janet` after runtime disposal | Underlying memory is freed |
| Stay on the creating thread | Janet is not thread-safe |
