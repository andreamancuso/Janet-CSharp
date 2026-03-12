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

## Phase 3: Complex Type System & Object Mapping

*Goal: Allow fluid mapping between C# classes/collections and Janet's rich data structures.*

* **3.1 Strings, Symbols, and Keywords**
* Implement zero-allocation UTF-8 string conversions using `ReadOnlySpan<byte>` and `MemoryMarshal`.
* Build wrapper classes: `JanetString`, `JanetSymbol`, `JanetKeyword` inheriting from `JanetValue`.

* **3.2 Tuples and Arrays (Indexed Data)**
* Implement `JanetArray` implementing `IList<JanetValue>`.
* Provide methods to push/pop from Janet arrays directly from C#.
* Implement `JanetTuple` (immutable arrays) mapping.

* **3.3 Tables and Structs (Key-Value Data)**
* Implement `JanetTable` implementing `IDictionary<JanetValue, JanetValue>`.
* Map C# `dynamic` or `ExpandoObject` to Janet Tables for rapid prototyping.

* **3.4 Buffers**
* Implement C# `Stream` wrappers around Janet Buffers to allow native .NET I/O operations directly into Janet memory space.

---

## Phase 4: Bi-Directional Function Calls

*Goal: Enable Janet to call C# methods, and C# to invoke Janet functions with arguments.*

* **4.1 Invoking Janet from C#**
* Implement `JanetFunction.Invoke(params JanetValue[] args)`.
* Handle the C-shim `janet_pcall` response, converting Janet panics into a custom `JanetRuntimeException` in C# containing the Janet stack trace.

* **4.2 Exposing C# to Janet (Callbacks)**
* Implement an `ExportToJanet` attribute for C# methods.
* Build a reflection/source-generator tool to automatically generate `JanetCFunction` delegates from C# methods.
* Implement the Delegate pinning architecture (using `GCHandle` or pinned lists) to prevent the .NET GC from collecting active callbacks.

* **4.3 Type Coercion Pipeline**
* Build a registry that automatically coerces arguments when Janet calls C#. (e.g., Janet Number -> C# `int`, Janet String -> C# `string`).

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

## Phase 6: Testing, Documentation & Polish

*Goal: Achieve production-readiness.*

* **6.1 Comprehensive Test Suite**
* Write xUnit tests verifying memory bounds, ensuring 0 bytes leaked after aggressive GC cycles.
* Test cross-thread access (Janet VMs are strictly single-threaded; `JanetRuntime` must enforce thread affinity or implement locking).

* **6.2 Documentation & Examples**
* Write a GitBook/DocFX site.
* Provide example projects: A Unity game script engine, an ASP.NET Core rules engine, and a console REPL.

* **6.3 CI/CD & Release**
* Set up GitHub Actions to matrix-build the C-shim for Windows, Linux, and macOS.
* Automate NuGet package generation and deployment.

---

This roadmap covers the entire lifecycle from bits and pointers up to asynchronous coroutines.
