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
* Implemented `JanetTable` implementing `IDictionary<Janet, Janet>` with get/set, remove, clear, ContainsKey, Keys, Values, GetEnumerator, CopyTo.
* Implemented `JanetStruct` implementing `IReadOnlyDictionary<Janet, Janet>` (immutable, created via Eval) with Keys, Values, GetEnumerator.
* Dictionary iteration via `shim_dictionary_collect` — snapshot-based enumeration using `janet_dictionary_next`.

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

## Phase 5: README & Quick-Start Documentation ✅

*Goal: Make the project approachable — a new developer should understand what JanetSharp is and how to use it within minutes.*

* **5.1 README.md**
* Project overview, badges (build status, NuGet version, license).
* Feature summary: what works today (Phases 1–4).
* Quick-start code sample: init runtime, eval, invoke functions, register callbacks.
* Installation instructions (NuGet + native shim prerequisites).
* Link to full docs and ROADMAP.

* **5.2 CONTRIBUTING.md**
* Build instructions (native shim via CMake, .NET build/test).
* Architecture overview for contributors (shim layer, NaN-boxing, GC rooting).
* How to add new shim functions end-to-end (C → P/Invoke → C# wrapper → test).

---

## Phase 6: Comprehensive Documentation ✅

*Goal: Provide full API documentation and usage guides so users can adopt JanetSharp without reading source code.*

* **6.1 API Reference** ✅
* XML doc comments on all public types and methods (Janet, JanetValue, JanetRuntime, all wrapper types, JanetFunction, JanetCallback, JanetConvert).
* Zero CS1591 warnings — all public members fully documented with `<summary>`, `<param>`, `<returns>`, and `<exception>` tags.
* DocFX site generation deferred — XML docs provide full IntelliSense coverage.

* **6.2 User Guide** ✅
* Six markdown guides in `docs/guide/`: Getting Started, Working with Types, Calling Janet from C#, Calling C# from Janet, Type Coercion, Memory Management.
* Index page at `docs/README.md` linking all guides and key types.

* **6.3 Examples Project**
* Standalone example projects demonstrating real-world usage patterns. Deferred.

---

## Phase 7: Testing & Hardening ✅

*Goal: Achieve production-level confidence in correctness and safety.*

* **7.1 Comprehensive Test Suite** ✅
* Dispose safety tests: double-dispose on all JanetValue subclasses, access after dispose.
* Type conversion error tests: invalid As* conversions, JanetConvert unsupported types.
* Callback slot exhaustion and recycling tests.
* Thread affinity enforcement tests.
* Edge cases: empty collections, nil values in tables, large argument lists, UTF-8, implicit conversions.
* Stress tests: GC pressure, callback hot loop, array/table/buffer churn, repeated eval.

* **7.2 GC Finalizer Race Condition Fix** ✅
* Added generation counter to `JanetRuntime` — each init increments a monotonic counter.
* `JanetValue` records its creation generation and only calls `shim_gcunroot` if the same-generation runtime is still active.
* `JanetRuntime.Dispose()` flushes pending finalizers (`GC.Collect` + `WaitForPendingFinalizers`) before calling `shim_deinit()`.

* **7.3 Cross-Platform Validation**
* Verify native shim builds and tests pass on Linux (`.so`) and macOS (`.dylib`).
* Document any platform-specific considerations.

---

## Phase 8: NuGet Packaging & CI/CD ✅

*Goal: Publish JanetSharp as a self-contained NuGet package with automated builds.*

* **8.1 NuGet Package Structure** ✅
* Multi-RID native packaging: `runtimes/win-x64/native/`, `runtimes/linux-x64/native/`.
* Package metadata: description, tags, license, repository URL, readme.
* `dotnet add package JanetSharp` works out of the box — no manual native build required.
* Fixed CMakeLists.txt with platform-specific link libraries (`ws2_32`, `m`, `pthread`, `dl`).
* **TODO:** macOS ARM64 (`osx-arm64`) — native shim crashes during `janet_init()` on Apple Silicon CI runners. Temporarily disabled.

* **8.2 CI/CD Pipeline** ✅
* GitHub Actions workflow (`.github/workflows/ci.yml`): matrix-build the C-shim for Windows and Linux.
* Run `dotnet test` on all platforms.
* Pack job assembles multi-RID NuGet package on pushes to `main` and version tags.
* Publish job pushes to nuget.org on `v*` tags using `NUGET_API_KEY` secret.

* **8.3 Versioning Strategy** ✅
* `Version` property in `Directory.Build.props` (default `0.1.0`).
* CI derives version from git tags (`v1.0.0` → `1.0.0`) or uses `0.1.0-ci.{run_number}` for main branch builds.

---

## Phase 9: Fibers & Advanced Features

*Goal: Bridge Janet's fiber (coroutine) system to C# and add advanced interop features.*

* **9.1 JanetFiber (Coroutine Support)** ✅
* Implemented `JanetFiber : JanetValue` with `Create(JanetFunction, params Janet[])`, `Resume()` (throwing and non-throwing variants), `Status`, and `CanResume`.
* Added `JanetFiberStatus` enum (Dead, Error, Debug, Pending, New, Alive).
* Added `AsFiber()` to the `Janet` struct for unwrapping fiber values.
* C-shim functions: `shim_fiber_new`, `shim_continue`, `shim_fiber_status`, `shim_fiber_can_resume`, `shim_unwrap_fiber`, `shim_wrap_fiber_value`.
* **Note:** `Task<JanetValue> RunAsync()` was originally planned but is not applicable — Janet is single-threaded and we build with `JANET_NO_EV` (no event loop). Fiber operations are synchronous.

* **9.2 Custom Abstract Types**
* Utilize Janet's `Abstract` types to wrap complex C# objects (like Database connections or UI Windows) inside Janet, fully managed by Janet's GC but finalized in C#.

* **9.3 Module Loading & VFS (Virtual File System)**
* Hook into Janet's `import` mechanism.
* Allow Janet scripts to load other Janet scripts embedded within .NET Assembly Resources, bypassing the physical disk.
