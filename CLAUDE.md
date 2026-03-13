# CLAUDE.md

## Project Overview
**JanetSharp** — a .NET-to-Janet bridge for embedding the Janet scripting language in C# applications.
Repo name is `Janet-CSharp`; library/namespace/NuGet name is `JanetSharp`.

## Build Instructions

### Native shim (requires VS 2022 Build Tools)
In Claude Code's Git Bash shell, `cmake` is not on PATH. Use `cmd.exe //c` to invoke it:
```bash
cmd.exe //c "cd /d C:\dev\Janet-CSharp\native && cmake -B build"
cmd.exe //c "cd /d C:\dev\Janet-CSharp\native && cmake --build build --config Release"
```
Produces `native/build/Release/janet_shim.dll` (or `.so`/`.dylib`).

The .csproj auto-discovers the DLL from CLI cmake paths first (`native/build/Release`, then `Debug`), falling back to VS IDE paths (`native/out/build/x64-*`).

### .NET
```bash
dotnet build
dotnet test
```

## Architecture

### C-shim layer (`native/janet_shim.c`)
Janet uses `setjmp`/`longjmp` for error handling. If a `longjmp` unwinds through a C# P/Invoke frame, the CLR crashes. The C-shim wraps **all** Janet API calls so panics are caught by `janet_pcall` (which uses `setjmp` internally) and converted to return codes. No Janet call should ever be made directly from C# without going through the shim.

### Janet build strategy
Janet is included as a git submodule at `extern/janet`. The CMake build uses a **two-stage bootstrap**:

1. **Stage 1**: Build `janet_boot` executable from `src/core/*.c` + `src/boot/*.c` with `-DJANET_BOOTSTRAP`.
2. **Stage 2**: Run `janet_boot` to execute `boot.janet`, which compiles the full Janet stdlib and outputs a serialized core image (`janet_core_image.c`).
3. **Stage 3**: Build `janet_shim.dll` from `src/core/*.c` + the generated image + `janet_shim.c` **without** `JANET_BOOTSTRAP`. The full Janet language is available (`defn`, `loop`, `map`, `match`, `import`, etc.).

### Disabled subsystems (`JANET_NO_*` flags)

Set on **both** `janet_boot` and `janet_shim` to keep the image consistent.

| Flag | What it disables | Why disabled |
|------|-----------------|-------------|
| `JANET_NO_EV` | Event loop, async channels, filewatch (33 primitives) | macOS ARM64 kqueue crash; spawns OS threads incompatible with single-thread enforcement; C# handles concurrency |
| `JANET_NO_FFI` | Foreign function interface (17 primitives) | JanetSharp *is* the FFI layer; security risk from loading arbitrary native libraries |
| `JANET_NO_NET` | Networking/sockets (17 primitives) | Depends on EV; C# provides superior networking APIs |
| `JANET_NO_DYNAMIC_MODULES` | `.so`/`.dll` native module loading | Security risk; NuGet handles distribution; Janet modules load as source via `(import)` |

Flags **not** set (kept enabled): `JANET_NO_ASSEMBLER`, `JANET_NO_PEG`, `JANET_NO_INT_TYPES` — useful features with no downside.

### Value marshalling
Janet values are 64-bit NaN-boxed unions. They cross the P/Invoke boundary as `long` (8 bytes). All Janet pointer types (tables, functions, fibers) are passed as `void*`/`IntPtr`.

## Key Types
- `JanetType` enum: NUMBER=0, NIL=1, BOOLEAN=2, FIBER=3, STRING=4, SYMBOL=5, KEYWORD=6, ARRAY=7, TUPLE=8, TABLE=9, STRUCT=10, BUFFER=11, FUNCTION=12, CFUNCTION=13, ABSTRACT=14, POINTER=15
- `JanetSignal` enum: OK=0, ERROR=1, DEBUG=2, YIELD=3

## Conventions
- Targets `net9.0` so the NuGet package can be consumed by .NET 9+ apps; solution file is traditional `.sln` format for .NET 9 SDK compatibility
- `LibraryImport` (source-generated P/Invoke) instead of `DllImport`
- `AllowUnsafeBlocks` enabled in Core project
- xUnit for tests
- CMake 3.15+ for native build
