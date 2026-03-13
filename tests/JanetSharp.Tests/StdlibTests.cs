using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Full Janet Standard Library Tests ===
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

    // ============================================================
    // Phase 10.4: Extended Stdlib Tests
    // ============================================================

    // --- Destructuring ---

    [Fact]
    public void Destructure_Array()
    {
        var result = _runtime.Eval("(let [[a b c] [10 20 30]] (+ a b c))");
        Assert.Equal(60.0, result.AsNumber());
    }

    [Fact]
    public void Destructure_RestArgs()
    {
        var result = _runtime.Eval("(let [[x & rest] [1 2 3 4]] (length rest))");
        Assert.Equal(3.0, result.AsNumber());
    }

    [Fact]
    public void Destructure_TableKeys()
    {
        var result = _runtime.Eval("(let [{:a a :b b} {:a 10 :b 20}] (+ a b))");
        Assert.Equal(30.0, result.AsNumber());
    }

    [Fact]
    public void Destructure_Nested()
    {
        var result = _runtime.Eval("(let [[[x] [y]] [[1] [2]]] (+ x y))");
        Assert.Equal(3.0, result.AsNumber());
    }

    [Fact]
    public void Destructure_DefnArgs()
    {
        var result = _runtime.Eval(@"
            (defn first-two [[a b & _]] [a b])
            (first-two [10 20 30 40])");
        Assert.Equal(JanetType.Tuple, result.Type);
        using var tup = result.AsTuple();
        Assert.Equal(10.0, tup[0].AsNumber());
        Assert.Equal(20.0, tup[1].AsNumber());
    }

    // --- Quasiquoting ---

    [Fact]
    public void Quasiquote_BasicUnquote()
    {
        var result = _runtime.Eval("(let [x 5] ~(+ 1 ,x))");
        Assert.Equal(JanetType.Tuple, result.Type);
        using var tup = result.AsTuple();
        Assert.Equal(3, tup.Count);
        Assert.Equal(5.0, tup[2].AsNumber());
    }

    [Fact]
    public void Quasiquote_Splice()
    {
        // ;x in quasiquote produces a (splice x) marker in the data;
        // verify the structure is correct
        var result = _runtime.Eval("(let [xs [2 3]] ~[1 ;xs 4])");
        Assert.Equal(JanetType.Tuple, result.Type);
        using var tup = result.AsTuple();
        // [1 (splice [2 3]) 4] — 3 elements: 1, the splice marker, and 4
        Assert.Equal(3, tup.Count);
        Assert.Equal(1.0, tup[0].AsNumber());
        Assert.Equal(4.0, tup[2].AsNumber());
    }

    [Fact]
    public void Quasiquote_InMacro()
    {
        var result = _runtime.Eval(@"
            (defmacro add-ten [x] ~(+ ,x 10))
            (add-ten 32)");
        Assert.Equal(42.0, result.AsNumber());
    }

    // --- String Operations ---

    [Fact]
    public void StringJoin_ConcatenatesWithSeparator()
    {
        var result = _runtime.Eval("(string/join [\"a\" \"b\" \"c\"] \",\")");
        Assert.Equal("a,b,c", result.AsString());
    }

    [Fact]
    public void StringSplit_SplitsString()
    {
        var result = _runtime.Eval("(string/split \",\" \"a,b,c\")");
        using var arr = result.AsArray();
        Assert.Equal(3, arr.Count);
        Assert.Equal("a", arr[0].AsString());
        Assert.Equal("b", arr[1].AsString());
        Assert.Equal("c", arr[2].AsString());
    }

    [Fact]
    public void StringTrim_RemovesWhitespace()
    {
        var result = _runtime.Eval("(string/trim \"  hello  \")");
        Assert.Equal("hello", result.AsString());
    }

    [Fact]
    public void StringHasPrefix_ReturnsBool()
    {
        Assert.True(_runtime.Eval("(string/has-prefix? \"he\" \"hello\")").AsBoolean());
        Assert.False(_runtime.Eval("(string/has-prefix? \"wo\" \"hello\")").AsBoolean());
    }

    [Fact]
    public void StringHasSuffix_ReturnsBool()
    {
        Assert.True(_runtime.Eval("(string/has-suffix? \"lo\" \"hello\")").AsBoolean());
        Assert.False(_runtime.Eval("(string/has-suffix? \"he\" \"hello\")").AsBoolean());
    }

    [Fact]
    public void StringRepeat_RepeatsString()
    {
        var result = _runtime.Eval("(string/repeat \"ab\" 3)");
        Assert.Equal("ababab", result.AsString());
    }

    // --- Table Operations ---

    [Fact]
    public void TableMerge_CombinesTables()
    {
        var result = _runtime.Eval("(merge @{:a 1} @{:b 2} @{:c 3})");
        Assert.Equal(JanetType.Table, result.Type);
        using var tbl = result.AsTable();
        using var ka = JanetKeyword.Create("a");
        using var kb = JanetKeyword.Create("b");
        using var kc = JanetKeyword.Create("c");
        Assert.Equal(1.0, tbl[ka.Value].AsNumber());
        Assert.Equal(2.0, tbl[kb.Value].AsNumber());
        Assert.Equal(3.0, tbl[kc.Value].AsNumber());
    }

    [Fact]
    public void TableClone_CreatesShallowCopy()
    {
        var result = _runtime.Eval(@"
            (let [orig @{:x 10}
                  copy (table/clone orig)]
              (put copy :x 99)
              (get orig :x))");
        Assert.Equal(10.0, result.AsNumber());
    }

    [Fact]
    public void TableToStruct_ConvertsToImmutable()
    {
        var result = _runtime.Eval("(table/to-struct @{:a 1 :b 2})");
        Assert.Equal(JanetType.Struct, result.Type);
        using var st = result.AsStruct();
        Assert.Equal(2, st.Count);
    }

    [Fact]
    public void Freeze_MakesTableImmutable()
    {
        var result = _runtime.Eval("(freeze @{:x 10})");
        Assert.Equal(JanetType.Struct, result.Type);
    }

    [Fact]
    public void Thaw_MakesStructMutable()
    {
        var result = _runtime.Eval("(thaw {:x 10})");
        Assert.Equal(JanetType.Table, result.Type);
        using var tbl = result.AsTable();
        using var kx = JanetKeyword.Create("x");
        Assert.Equal(10.0, tbl[kx.Value].AsNumber());
    }

    // --- Math ---

    [Fact]
    public void MathFloor_RoundsDown()
    {
        Assert.Equal(3.0, _runtime.Eval("(math/floor 3.7)").AsNumber());
        Assert.Equal(-4.0, _runtime.Eval("(math/floor -3.2)").AsNumber());
    }

    [Fact]
    public void MathCeil_RoundsUp()
    {
        Assert.Equal(4.0, _runtime.Eval("(math/ceil 3.2)").AsNumber());
        Assert.Equal(-3.0, _runtime.Eval("(math/ceil -3.7)").AsNumber());
    }

    [Fact]
    public void MathAbs_ReturnsAbsoluteValue()
    {
        Assert.Equal(5.0, _runtime.Eval("(math/abs -5)").AsNumber());
        Assert.Equal(5.0, _runtime.Eval("(math/abs 5)").AsNumber());
    }

    [Fact]
    public void MathSqrt_ReturnsSquareRoot()
    {
        Assert.Equal(4.0, _runtime.Eval("(math/sqrt 16)").AsNumber());
        Assert.Equal(3.0, _runtime.Eval("(math/sqrt 9)").AsNumber());
    }

    [Fact]
    public void MathPow_RaisesToPower()
    {
        Assert.Equal(8.0, _runtime.Eval("(math/pow 2 3)").AsNumber());
        Assert.Equal(1.0, _runtime.Eval("(math/pow 5 0)").AsNumber());
    }

    [Fact]
    public void MathPi_IsAvailable()
    {
        var pi = _runtime.Eval("math/pi").AsNumber();
        Assert.True(pi > 3.14 && pi < 3.15);
    }

    // --- Sorting ---

    [Fact]
    public void Sort_SortsInPlace()
    {
        var result = _runtime.Eval("(sort @[3 1 4 1 5 9 2 6])");
        using var arr = result.AsArray();
        Assert.Equal(1.0, arr[0].AsNumber());
        Assert.Equal(1.0, arr[1].AsNumber());
        Assert.Equal(2.0, arr[2].AsNumber());
        Assert.Equal(3.0, arr[3].AsNumber());
    }

    [Fact]
    public void Sorted_ReturnsNewSortedArray()
    {
        var result = _runtime.Eval("(sorted [3 1 2])");
        using var arr = result.AsArray();
        Assert.Equal(3, arr.Count);
        Assert.Equal(1.0, arr[0].AsNumber());
        Assert.Equal(2.0, arr[1].AsNumber());
        Assert.Equal(3.0, arr[2].AsNumber());
    }

    [Fact]
    public void SortBy_SortsByKeyFunction()
    {
        var result = _runtime.Eval("(sorted-by |(- $) [3 1 2])");
        using var arr = result.AsArray();
        Assert.Equal(3, arr.Count);
        Assert.Equal(3.0, arr[0].AsNumber());
        Assert.Equal(2.0, arr[1].AsNumber());
        Assert.Equal(1.0, arr[2].AsNumber());
    }

    // --- Fiber + Stdlib ---

    [Fact]
    public void Fiber_GeneratorWithEachAndYield()
    {
        var result = _runtime.Eval(@"
            (defn gen []
              (each x [10 20 30]
                (yield x)))
            (def f (fiber/new gen :yi))
            (var sum 0)
            (loop [v :in f] (set sum (+ sum v)))
            sum");
        Assert.Equal(60.0, result.AsNumber());
    }

    [Fact]
    public void Fiber_GenerateMacro()
    {
        var result = _runtime.Eval(@"
            (def g (generate [i :range [0 5]] (* i i)))
            (var sum 0)
            (loop [v :in g] (set sum (+ sum v)))
            sum");
        // 0 + 1 + 4 + 9 + 16 = 30
        Assert.Equal(30.0, result.AsNumber());
    }

    [Fact]
    public void Fiber_MapOverGenerator()
    {
        var result = _runtime.Eval(@"
            (defn squares []
              (for i 1 4 (yield (* i i))))
            (def f (fiber/new squares :yi))
            (def results @[])
            (loop [v :in f] (array/push results v))
            results");
        using var arr = result.AsArray();
        Assert.Equal(3, arr.Count);
        Assert.Equal(1.0, arr[0].AsNumber());
        Assert.Equal(4.0, arr[1].AsNumber());
        Assert.Equal(9.0, arr[2].AsNumber());
    }

    [Fact]
    public void Fiber_ResumeWithStdlib()
    {
        var result = _runtime.Eval(@"
            (def f (fiber/new (fn []
              (let [x (yield :ready)]
                (string ""got: "" x))) :yi))
            (resume f)
            (resume f ""hello"")");
        Assert.Equal("got: hello", result.AsString());
    }

    // --- Error Handling ---

    [Fact]
    public void Protect_CatchesError()
    {
        var result = _runtime.Eval("(protect (error \"oops\"))");
        Assert.Equal(JanetType.Tuple, result.Type);
        using var tup = result.AsTuple();
        Assert.False(tup[0].AsBoolean()); // false = error
        Assert.Equal("oops", tup[1].AsString());
    }

    [Fact]
    public void Protect_SuccessReturnsTrue()
    {
        var result = _runtime.Eval("(protect (+ 1 2))");
        Assert.Equal(JanetType.Tuple, result.Type);
        using var tup = result.AsTuple();
        Assert.True(tup[0].AsBoolean()); // true = success
        Assert.Equal(3.0, tup[1].AsNumber());
    }

    [Fact]
    public void TryCatch_HandlesError()
    {
        var result = _runtime.Eval(@"
            (try
              (error ""fail"")
              ([err] (string ""caught: "" err)))");
        Assert.Equal("caught: fail", result.AsString());
    }

    // --- Pretty Print / Describe ---

    [Fact]
    public void Describe_ReturnsStringRepresentation()
    {
        // describe on primitives returns a readable string
        var result = _runtime.Eval("(describe :hello)");
        Assert.Equal(JanetType.String, result.Type);
        Assert.Equal(":hello", result.AsString());

        var numResult = _runtime.Eval("(describe 42)");
        Assert.Equal("42", numResult.AsString());
    }

    [Fact]
    public void String_Concatenation()
    {
        var result = _runtime.Eval("(string \"hello\" \" \" \"world\")");
        Assert.Equal("hello world", result.AsString());
    }

    // --- Var/Set Mutation ---

    [Fact]
    public void DoBlock_ReturnsLastExpression()
    {
        var result = _runtime.Eval("(do 1 2 3)");
        Assert.Equal(3.0, result.AsNumber());
    }

    [Fact]
    public void VarSet_MutatesBinding()
    {
        var result = _runtime.Eval("(var x 0) (set x 42) x");
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void VarSet_AccumulatorLoop()
    {
        var result = _runtime.Eval(@"
            (var total 0)
            (for i 1 6 (set total (+ total i)))
            total");
        // 1+2+3+4+5 = 15
        Assert.Equal(15.0, result.AsNumber());
    }

    // --- Misc ---

    [Fact]
    public void Range_GeneratesSequence()
    {
        var result = _runtime.Eval("(range 5)");
        using var arr = result.AsArray();
        Assert.Equal(5, arr.Count);
        Assert.Equal(0.0, arr[0].AsNumber());
        Assert.Equal(4.0, arr[4].AsNumber());
    }

    [Fact]
    public void Zipcoll_PairsKeysAndValues()
    {
        var result = _runtime.Eval("(zipcoll [:a :b :c] [1 2 3])");
        Assert.Equal(JanetType.Table, result.Type);
        using var tbl = result.AsTable();
        using var ka = JanetKeyword.Create("a");
        using var kb = JanetKeyword.Create("b");
        using var kc = JanetKeyword.Create("c");
        Assert.Equal(1.0, tbl[ka.Value].AsNumber());
        Assert.Equal(2.0, tbl[kb.Value].AsNumber());
        Assert.Equal(3.0, tbl[kc.Value].AsNumber());
    }
}
