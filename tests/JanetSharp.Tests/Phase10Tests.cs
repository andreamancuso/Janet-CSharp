using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Full Janet Standard Library Tests (Phase 10) ===
// These tests verify that the boot.janet stdlib is available
// now that JANET_BOOTSTRAP has been removed.

public class StdlibTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public StdlibTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    // --- Macros ---

    [Fact]
    public void Defn_DefinesAndCallsFunction()
    {
        var result = _runtime.Eval("(defn square [x] (* x x)) (square 7)");
        Assert.Equal(49.0, result.AsNumber());
    }

    [Fact]
    public void Let_BindsLocalVariables()
    {
        var result = _runtime.Eval("(let [x 10 y 20] (+ x y))");
        Assert.Equal(30.0, result.AsNumber());
    }

    [Fact]
    public void When_ExecutesOnTruthy()
    {
        var result = _runtime.Eval("(when true 42)");
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void When_ReturnsNilOnFalsy()
    {
        var result = _runtime.Eval("(when false 42)");
        Assert.True(result.IsNil);
    }

    [Fact]
    public void Unless_ExecutesOnFalsy()
    {
        var result = _runtime.Eval("(unless false 99)");
        Assert.Equal(99.0, result.AsNumber());
    }

    [Fact]
    public void Cond_SelectsCorrectBranch()
    {
        var result = _runtime.Eval("(cond false 1 true 2 3)");
        Assert.Equal(2.0, result.AsNumber());
    }

    [Fact]
    public void Case_MatchesValue()
    {
        var result = _runtime.Eval("(case 2 1 :one 2 :two :other)");
        Assert.Equal(JanetType.Keyword, result.Type);
        Assert.Equal("two", result.AsString());
    }

    [Fact]
    public void IfLet_BindsAndBranches()
    {
        var result = _runtime.Eval("(if-let [x (get {:a 1} :a)] (+ x 10) 0)");
        Assert.Equal(11.0, result.AsNumber());
    }

    // --- Iteration ---

    [Fact]
    public void Loop_Iterates()
    {
        var result = _runtime.Eval(@"
            (var sum 0)
            (loop [i :range [0 5]] (set sum (+ sum i)))
            sum");
        Assert.Equal(10.0, result.AsNumber());
    }

    [Fact]
    public void For_Iterates()
    {
        var result = _runtime.Eval(@"
            (var sum 0)
            (for i 1 4 (set sum (+ sum i)))
            sum");
        Assert.Equal(6.0, result.AsNumber());
    }

    [Fact]
    public void Each_IteratesArray()
    {
        var result = _runtime.Eval(@"
            (var sum 0)
            (each x [10 20 30] (set sum (+ sum x)))
            sum");
        Assert.Equal(60.0, result.AsNumber());
    }

    // --- Functional Combinators ---

    [Fact]
    public void Map_TransformsElements()
    {
        var result = _runtime.Eval("(map |(* $ 2) [1 2 3])");
        Assert.Equal(JanetType.Array, result.Type);
        using var arr = result.AsArray();
        Assert.Equal(3, arr.Count);
        Assert.Equal(2.0, arr[0].AsNumber());
        Assert.Equal(4.0, arr[1].AsNumber());
        Assert.Equal(6.0, arr[2].AsNumber());
    }

    [Fact]
    public void Filter_SelectsElements()
    {
        var result = _runtime.Eval("(filter |(> $ 2) [1 2 3 4 5])");
        Assert.Equal(JanetType.Array, result.Type);
        using var arr = result.AsArray();
        Assert.Equal(3, arr.Count);
        Assert.Equal(3.0, arr[0].AsNumber());
        Assert.Equal(4.0, arr[1].AsNumber());
        Assert.Equal(5.0, arr[2].AsNumber());
    }

    [Fact]
    public void Reduce_AccumulatesValue()
    {
        var result = _runtime.Eval("(reduce + 0 [1 2 3 4])");
        Assert.Equal(10.0, result.AsNumber());
    }

    [Fact]
    public void Keep_FiltersAndTransforms()
    {
        var result = _runtime.Eval("(keep |(if (even? $) (* $ 10)) [1 2 3 4])");
        using var arr = result.AsArray();
        Assert.Equal(2, arr.Count);
        Assert.Equal(20.0, arr[0].AsNumber());
        Assert.Equal(40.0, arr[1].AsNumber());
    }

    [Fact]
    public void Find_ReturnsFirstMatch()
    {
        var result = _runtime.Eval("(find |(> $ 3) [1 2 3 4 5])");
        Assert.Equal(4.0, result.AsNumber());
    }

    // --- Pattern Matching ---

    [Fact]
    public void Match_MatchesLiteral()
    {
        var result = _runtime.Eval("(match 42 42 :yes _ :no)");
        Assert.Equal("yes", result.AsString());
    }

    [Fact]
    public void Match_BindsVariable()
    {
        var result = _runtime.Eval("(match [1 2 3] [a b c] (+ a b c))");
        Assert.Equal(6.0, result.AsNumber());
    }

    // --- String Formatting ---

    [Fact]
    public void StringFormat_FormatsValues()
    {
        var result = _runtime.Eval("(string/format \"%d + %d = %d\" 1 2 3)");
        Assert.Equal("1 + 2 = 3", result.AsString());
    }

    // --- Defmacro ---

    [Fact]
    public void Defmacro_DefinesCustomMacro()
    {
        var result = _runtime.Eval(@"
            (defmacro double-eval [x] ~(+ ,x ,x))
            (double-eval 21)");
        Assert.Equal(42.0, result.AsNumber());
    }

    // --- Short-fn Syntax ---

    [Fact]
    public void ShortFn_Works()
    {
        var result = _runtime.Eval("(map |(string $ \"!\") [\"a\" \"b\" \"c\"])");
        using var arr = result.AsArray();
        Assert.Equal("a!", arr[0].AsString());
        Assert.Equal("b!", arr[1].AsString());
        Assert.Equal("c!", arr[2].AsString());
    }

    // --- Misc Stdlib ---

    [Fact]
    public void Apply_SplatsArguments()
    {
        var result = _runtime.Eval("(apply + [1 2 3 4])");
        Assert.Equal(10.0, result.AsNumber());
    }

    [Fact]
    public void Interpose_InsertsSeperator()
    {
        var result = _runtime.Eval("(interpose :sep [1 2 3])");
        Assert.Equal(JanetType.Array, result.Type);
        using var arr = result.AsArray();
        Assert.Equal(5, arr.Count);
    }

    [Fact]
    public void Frequencies_CountsOccurrences()
    {
        var result = _runtime.Eval("(frequencies [1 2 1 3 2 1])");
        Assert.Equal(JanetType.Table, result.Type);
        using var tbl = result.AsTable();
        Assert.Equal(3.0, tbl[Janet.From(1.0)].AsNumber());
        Assert.Equal(2.0, tbl[Janet.From(2.0)].AsNumber());
        Assert.Equal(1.0, tbl[Janet.From(3.0)].AsNumber());
    }
}
