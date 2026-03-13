# Performance Baseline (Phase 10.7)

Performance baselines for the full-stdlib two-stage boot build of JanetSharp, measured with BenchmarkDotNet v0.14.0.

## Environment

| Component | Value |
|-----------|-------|
| CPU | Intel Core i7-9750H @ 2.60 GHz, 6 cores / 12 threads |
| OS | Windows 11 (10.0.26200.8037) |
| .NET SDK | 10.0.103 |
| Runtime | .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2 |
| GC | Concurrent Workstation |

## Results

### Startup

| Benchmark | Mean | Error | StdDev | Allocated |
|-----------|-----:|------:|-------:|----------:|
| RuntimeStartup | 1.736 ms | 0.034 ms | 0.087 ms | 440 B |

Runtime initialization includes `janet_init()`, image unmarshal (full stdlib), and core environment creation. Under 2 ms is fast enough for application startup; unsuitable only for per-request initialization patterns.

### Expression Evaluation

| Benchmark | Mean | Error | StdDev | Allocated |
|-----------|-----:|------:|-------:|----------:|
| EvalArithmetic | 3.20 us | 0.056 us | 0.052 us | - |
| EvalStringConcat | 4.85 us | 0.097 us | 0.159 us | - |
| EvalDefnAndCall | 14.66 us | 0.288 us | 0.432 us | - |
| EvalMapFilter | 67.95 us | 1.348 us | 2.137 us | - |
| EvalLoop | 62.38 us | 1.223 us | 1.360 us | - |

Each `Eval()` call parses, compiles, and executes the Janet source string. The cost scales with expression complexity:
- Simple arithmetic: ~3 us (parse + compile + one opcode)
- String operations: ~5 us (adds string allocation)
- Function definition + call: ~15 us (creates closure, then invokes)
- Stdlib combinators over 100 elements: ~68 us (map + filter pipeline)
- Tight loop (1000 iterations): ~62 us (~62 ns per iteration)

Zero managed allocations across all eval benchmarks — all work happens in Janet's native heap.

### Function Call Overhead

| Benchmark | Mean | Error | StdDev | Median | Allocated |
|-----------|-----:|------:|-------:|-------:|----------:|
| InvokeJanetFunction | 958 ns | 95 ns | 276 ns | 878 ns | - |
| CallbackRoundTrip | 3,955 ns | 78 ns | 129 ns | 3,921 ns | - |

- **InvokeJanetFunction**: Calls the pre-resolved `+` function via `JanetFunction.Invoke()`. Sub-microsecond median, though variance is high due to OS scheduling noise at this timescale. The ~880 ns median is a better indicator than the mean.
- **CallbackRoundTrip**: Janet evaluates `(bench-callback 42)`, which calls a registered C# callback that returns `args[0] + 1`. The ~4 us cost includes eval parsing overhead; the actual C#-to-Janet boundary crossing is the difference from EvalArithmetic (~3.2 us), roughly ~0.8 us for the callback dispatch.

### Memory

All benchmarks show zero managed allocations per operation (the `-` in the Allocated column). The `Janet` struct is a stack-allocated 8-byte value type, and function invocation uses stackalloc or pre-allocated arrays. The only managed allocation is 440 bytes during runtime startup.

## Native DLL Size

| File | Size |
|------|-----:|
| `janet_shim.dll` (Release, Windows x64) | 661 KB |

This includes all Janet core C sources, the serialized stdlib image (`janet_core_image.c`), and the C-shim wrapper functions. The stdlib image accounts for roughly 150-200 KB of this total.

## Running Benchmarks

```bash
cd benchmarks/JanetSharp.Benchmarks
dotnet run -c Release -- --filter "*"
```

To run a specific benchmark class:
```bash
dotnet run -c Release -- --filter "*EvalBenchmarks*"
```
