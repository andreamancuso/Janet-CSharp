using BenchmarkDotNet.Attributes;

namespace JanetSharp.Benchmarks;

/// <summary>
/// Measures JanetRuntime initialization cost (janet_init + image unmarshal + core_env creation).
/// Uses IterationSetup/Cleanup since each iteration must create and destroy the singleton runtime.
/// </summary>
[MemoryDiagnoser]
public class StartupBenchmarks
{
    private JanetRuntime? _runtime;

    [IterationCleanup]
    public void Cleanup()
    {
        _runtime?.Dispose();
        _runtime = null;
    }

    [Benchmark]
    public void RuntimeStartup()
    {
        _runtime = new JanetRuntime();
    }
}
