# ROADMAP.md: JanetNet

## Vision

To create a seamless, high-performance, and garbage-collection-safe bridge between the .NET CLR and the Janet programming language. `JanetNet` will allow C# developers to embed Janet as a scripting engine, execute coroutines, and share complex data structures without memory leaks or runtime segmentation faults.

---

## Phase 1: Foundation & Cross-Platform Toolchain

*Goal: Establish the native build pipeline, C-shim architecture, and NuGet packaging strategy for multi-OS support.*

* **1.1 Repository & Project Structure Setup**
* Initialize the solution with separate projects: `JanetNet.Core` (C#), `JanetNet.Native` (C-Shim), and `JanetNet.Tests`.
* Submodule the official `janet-lang/janet` repository to pin the exact native version.


* **1.2 Native C-Shim Build System (CMake)**
* Create a `CMakeLists.txt` that compiles `janet.c` alongside `janet_shim.c`.
* Configure targets for Windows (`.dll`), Linux (`.so`), and macOS (`.dylib` / x64 & ARM64).
* Export a clean, stable ABI (Application Binary Interface) strictly using `__cdecl`.


* **1.3 Safe Execution Wrapper (Crucial)**
* Implement `shim_janet_pcall` in the C-shim. Janet uses `setjmp`/`longjmp` for errors (panics). If a `longjmp` bypasses a C# P/Invoke frame, the CLR will crash. The shim must intercept *all* executions via `janet_pcall` and translate panics into standard C error codes to be thrown as C# `Exceptions`.


* **1.4 NuGet Native Packaging Strategy**
* Configure the `.csproj` to map native binaries into the correct `runtimes/{rid}/native/` folders inside the resulting `.nupkg` (e.g., `win-x64`, `linux-x64`, `osx-arm64`).



---

## Phase 2: Core Interop & Memory Management

*Goal: Map the primitive types and establish the bridge between the Janet GC and the .NET GC.*

* **2.1 Primitive Marshalling**
* Finalize the `Janet` struct (`LayoutKind.Explicit`) for 64-bit NaN-boxing.
* Implement P/Invoke bindings for basic C-shim functions (Wrap/Unwrap for Numbers, Booleans, and Pointers).


* **2.2 The Smart Pointer (`JanetValue`)**
* Design the `JanetValue : IDisposable` class.
* Implement `janet_gcroot` in the constructor/initializer and `janet_gcunroot` in the `Dispose()` method and finalizer (`~JanetValue()`).
* Implement an internal thread-safe tracking mechanism (like a `ConditionalWeakTable`) to ensure duplicate C# references to the same Janet pointer share a single GC root.


* **2.3 The Runtime Lifecycle**
* Implement `JanetRuntime.cs`.
* Handle `janet_init()` and `janet_deinit()`.
* Manage the core environment (`janet_core_env`) and custom environments.



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

Because the single biggest failure point in native interop is exception handling crossing boundaries, would you like me to draft out the C-shim code for the **Safe Execution Wrapper** (Step 1.3) using `janet_pcall` so we can guarantee Janet panics never crash the CLR?
