# ROADMAP.md: JanetSharp

## Vision

To create a seamless, high-performance, and garbage-collection-safe bridge between the .NET CLR and the Janet programming language. `JanetSharp` will allow C# developers to embed Janet as a scripting engine, execute coroutines, and share complex data structures without memory leaks or runtime segmentation faults.

---

## Phase 1: Foundation & Cross-Platform Toolchain ✅

*Goal: Establish the native build pipeline, C-shim architecture, and NuGet packaging strategy for multi-OS support.*

* **1.1 Repository & Project Structure Setup** ✅
* Initialized the solution with `JanetSharp.Core` (C#), native C-shim, and `JanetSharp.Tests`.
* Submoduled the official `janet-lang/janet` repository at `extern/janet`.

* **1.2 Native C-Shim Build System (CMake)** ✅
* Created `native/CMakeLists.txt` compiling `src/core/*.c` with `JANET_BOOTSTRAP` alongside `janet_shim.c`.
* Configured targets for Windows (`.dll`), Linux (`.so`), and macOS (`.dylib`).
* Exported a clean, stable ABI using `__cdecl`.

* **1.3 Safe Execution Wrapper (Crucial)** ✅
* Implemented `shim_pcall` and `shim_dostring` in the C-shim. Janet's `setjmp`/`longjmp` panics are caught by `janet_pcall` internally and never cross a P/Invoke boundary.

* **1.4 NuGet Native Packaging Strategy** ✅
* Configured the `.csproj` to copy native binaries to the output directory. Runtime-specific `runtimes/{rid}/native/` mapping ready for NuGet packaging.

---

## Phase 2: Core Interop & Memory Management ✅

*Goal: Map the primitive types and establish the bridge between the Janet GC and the .NET GC.*

* **2.1 Primitive Marshalling** ✅
* Finalized the `Janet` struct (`LayoutKind.Explicit`, 8 bytes) for 64-bit NaN-boxing.
* Implemented `JanetType` and `JanetSignal` enums.
* Factory methods and unwrap methods for Numbers, Booleans, Nil, and Integers.

* **2.2 The Smart Pointer (`JanetValue`)** ✅
* Designed `JanetValue : IDisposable` with `janet_gcroot`/`janet_gcunroot` lifecycle.
* Skips GC rooting for primitive types (Number, Nil, Boolean) that don't participate in Janet's GC.
* Thread-safe dispose via `Interlocked.CompareExchange`.

* **2.3 The Runtime Lifecycle** ✅
* Implemented `JanetRuntime` managing `janet_init()`/`janet_deinit()` and `janet_core_env`.
* Singleton enforcement — only one runtime instance at a time.
* Thread affinity checks — Janet is single-threaded.
* `Eval()` methods with both throwing and non-throwing variants.
* `JanetException` for error propagation.

---

## Phase 3: Complex Type System & Object Mapping ✅

*Goal: Allow fluid mapping between C# classes/collections and Janet's rich data structures.*

* **3.1 Strings, Symbols, and Keywords** ✅
* Implemented `JanetString`, `JanetSymbol`, `JanetKeyword` inheriting from `JanetValue`.
* Zero-copy UTF-8 access via `ReadOnlySpan<byte> AsSpan()` and `ToString()` for .NET string conversion.
* C-shim functions: `shim_wrap_string/symbol/keyword`, `shim_unwrap_string_ptr`, `shim_string_length`.

* **3.2 Tuples and Arrays (Indexed Data)** ✅
* Implemented `JanetArray` implementing `IList<Janet>` with push, pop, indexer, enumeration.
* Implemented `JanetTuple` implementing `IReadOnlyList<Janet>` (immutable).
* C-shim functions for array CRUD and tuple creation/access.

* **3.3 Tables and Structs (Key-Value Data)** ✅
* Implemented `JanetTable` implementing `IDictionary<Janet, Janet>` with get/set, remove, clear, ContainsKey.
* Implemented `JanetStruct` implementing `IReadOnlyDictionary<Janet, Janet>` (immutable, created via Eval).
* Enumeration deferred — requires table/struct iteration support in the shim.

* **3.4 Buffers** ✅
* Implemented `JanetBuffer` with `WriteByte`, `WriteBytes(ReadOnlySpan<byte>)`, `AsSpan()`, `SetCount`, `EnsureCapacity`.
* Stream wrapper deferred to a future phase.

* **3.5 Convenience Methods** ✅
* Added `Janet.From(string)`, implicit conversions (`double`, `int`, `bool`, `string` → `Janet`).
* Added `AsString()`, `AsArray()`, `AsTuple()`, `AsTable()`, `AsStruct()`, `AsBuffer()` on `Janet` struct.

---

## Phase 4: Bi-Directional Function Calls ✅

*Goal: Enable Janet to call C# methods, and C# to invoke Janet functions with arguments.*

* **4.1 Invoking Janet from C#** ✅
* Implemented `JanetFunction : JanetValue` with `Invoke(params Janet[] args)` (throwing) and `Invoke(Janet[], out JanetSignal)` (non-throwing).
* Uses existing `shim_pcall` via `shim_unwrap_function` to extract the `JanetFunction*` pointer.
* Added `JanetRuntime.GetFunction(string)` convenience method.

* **4.2 Exposing C# to Janet (Callbacks)** ✅
* Implemented `JanetCallback : IDisposable` with `CallbackFunc` delegate (`ReadOnlySpan<Janet> → Janet`).
* 64-slot C-side trampoline system (macro-generated) prevents `longjmp` from unwinding through managed frames.
* Delegates pinned via `GCHandle`; C# exceptions caught and converted to `janet_panicv` calls in C.
* `JanetRuntime.Register(string, CallbackFunc)` registers named functions in the core environment via `shim_def`.

* **4.3 Type Coercion Pipeline** ✅
* Implemented `JanetConvert.ToJanet(object?)` and `JanetConvert.ToClr<T>(Janet)` for automatic conversion between .NET types and Janet values.
* Supports: `double`, `int`, `float`, `long`, `bool`, `string`, `null` ↔ Janet Number, Boolean, String, Nil.

---

## Phase 5: Concurrency & Advanced Features

*Goal: Bridge Janet's unique Fiber (coroutine) architecture with .NET's Task Parallel Library (TPL).*

* **5.1 Fiber to Task Mapping**
* Implement `JanetFiber`.
* Create an extension method `Task<JanetValue> RunAsync(this JanetFiber fiber)`.
* Map Janet's `yield` / `resume` state machine to the C# `async/await` state machine, allowing a C# Task to `await` a Janet coroutine.

* **5.2 Custom Abstract Types**
* Utilize Janet's `Abstract` types to wrap complex C# objects (like Database connections or UI Windows) inside Janet, fully managed by Janet's GC but finalized in C#.

* **5.3 Module Loading & VFS (Virtual File System)**
* Hook into Janet's `import` mechanism.
* Allow Janet scripts to load other Janet scripts embedded within .NET Assembly Resources, bypassing the physical disk.

---

## Phase 6: README & Quick-Start Documentation

*Goal: Make the project approachable — a new developer should understand what JanetSharp is and how to use it within minutes.*

* **6.1 README.md**
* Project overview, badges (build status, NuGet version, license).
* Feature summary: what works today (Phases 1–4).
* Quick-start code sample: init runtime, eval, invoke functions, register callbacks.
* Installation instructions (NuGet + native shim prerequisites).
* Link to full docs and ROADMAP.

* **6.2 CONTRIBUTING.md**
* Build instructions (native shim via CMake, .NET build/test).
* Architecture overview for contributors (shim layer, NaN-boxing, GC rooting).
* How to add new shim functions end-to-end (C → P/Invoke → C# wrapper → test).

---

## Phase 7: Comprehensive Documentation

*Goal: Provide full API documentation and usage guides so users can adopt JanetSharp without reading source code.*

* **7.1 API Reference**
* XML doc comments on all public types and methods (Janet, JanetValue, JanetRuntime, all wrapper types, JanetFunction, JanetCallback, JanetConvert).
* Generate API reference site via DocFX or similar tooling.

* **7.2 User Guide**
* Getting Started: installation, first script, basic eval.
* Working with Types: strings, arrays, tuples, tables, structs, buffers — with code samples.
* Calling Janet from C#: JanetFunction.Invoke, error handling, signals.
* Calling C# from Janet: JanetCallback, delegate pinning, exception safety.
* Type Coercion: JanetConvert usage and supported type mappings.
* Memory Management: GC rooting rules, when to dispose, common pitfalls.

* **7.3 Examples Project**
* Standalone example projects demonstrating real-world usage patterns.
* Console REPL, scripted rules engine, or configuration-driven application.

---

## Phase 8: Testing & Hardening

*Goal: Achieve production-level confidence in correctness and safety.*

* **8.1 Comprehensive Test Suite**
* Stress tests: aggressive GC cycles, verify 0 bytes leaked.
* Thread affinity enforcement tests.
* Callback slot exhaustion and recycling tests.
* Edge cases: empty collections, nil values in tables, large argument lists.

* **8.2 Cross-Platform Validation**
* Verify native shim builds and tests pass on Linux (`.so`) and macOS (`.dylib`).
* Document any platform-specific considerations.

---

## Phase 9: NuGet Packaging & CI/CD

*Goal: Publish JanetSharp as a self-contained NuGet package with automated builds.*

* **9.1 NuGet Package Structure**
* Multi-RID native packaging: `runtimes/win-x64/native/`, `runtimes/linux-x64/native/`, `runtimes/osx-x64/native/`.
* Package metadata: description, tags, license, repository URL, icon.
* Ensure `dotnet add package JanetSharp` works out of the box — no manual native build required.

* **9.2 CI/CD Pipeline**
* GitHub Actions workflow: matrix-build the C-shim for Windows, Linux, and macOS.
* Run `dotnet test` on all platforms.
* Automate NuGet package generation on tagged releases.
* Publish to nuget.org.

* **9.3 Versioning Strategy**
* Semantic versioning aligned with the roadmap phases.
* Pre-release tags for in-progress phases (e.g., `1.0.0-alpha.1`).

---

## Phase 10: Concurrency & Advanced Features

*Goal: Bridge Janet's unique Fiber (coroutine) architecture with .NET's Task Parallel Library (TPL).*

* **10.1 Fiber to Task Mapping**
* Implement `JanetFiber`.
* Create an extension method `Task<JanetValue> RunAsync(this JanetFiber fiber)`.
* Map Janet's `yield` / `resume` state machine to the C# `async/await` state machine, allowing a C# Task to `await` a Janet coroutine.

* **10.2 Custom Abstract Types**
* Utilize Janet's `Abstract` types to wrap complex C# objects (like Database connections or UI Windows) inside Janet, fully managed by Janet's GC but finalized in C#.

* **10.3 Module Loading & VFS (Virtual File System)**
* Hook into Janet's `import` mechanism.
* Allow Janet scripts to load other Janet scripts embedded within .NET Assembly Resources, bypassing the physical disk.
