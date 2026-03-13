using BenchmarkDotNet.Attributes;

namespace JanetSharp.Benchmarks;

/// <summary>
/// Measures C#-to-Janet and Janet-to-C# function call overhead.
/// </summary>
[MemoryDiagnoser]
public class FunctionCallBenchmarks
{
    private JanetRuntime _runtime = null!;
    private JanetFunction _addFn = null!;
    private JanetCallback _callback = null!;
    private Janet[] _args = null!;

    [GlobalSetup]
    public void Setup()
    {
        _runtime = new JanetRuntime();
        _addFn = _runtime.GetFunction("+");
        _args = [Janet.From(3.0), Janet.From(4.0)];
        _callback = _runtime.Register("bench-callback", args =>
            Janet.From(args[0].AsNumber() + 1));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _callback.Dispose();
        _addFn.Dispose();
        _runtime.Dispose();
    }

    [Benchmark]
    public Janet InvokeJanetFunction() => _addFn.Invoke(_args);

    [Benchmark]
    public Janet CallbackRoundTrip() => _runtime.Eval("(bench-callback 42)");
}
