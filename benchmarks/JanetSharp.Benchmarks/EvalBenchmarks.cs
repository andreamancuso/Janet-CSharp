using BenchmarkDotNet.Attributes;

namespace JanetSharp.Benchmarks;

/// <summary>
/// Measures expression evaluation latency across a range of Janet operations.
/// </summary>
[MemoryDiagnoser]
public class EvalBenchmarks
{
    private JanetRuntime _runtime = null!;

    [GlobalSetup]
    public void Setup()
    {
        _runtime = new JanetRuntime();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _runtime.Dispose();
    }

    [Benchmark]
    public Janet EvalArithmetic() => _runtime.Eval("(+ 1 2)");

    [Benchmark]
    public Janet EvalStringConcat() => _runtime.Eval("""(string "hello" " " "world")""");

    [Benchmark]
    public Janet EvalDefnAndCall() => _runtime.Eval("(defn f [x] (* x x)) (f 7)");

    [Benchmark]
    public Janet EvalMapFilter() => _runtime.Eval("(filter even? (map |(* $ 2) (range 100)))");

    [Benchmark]
    public Janet EvalLoop() => _runtime.Eval("(var s 0) (for i 0 1000 (set s (+ s i))) s");
}
