using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === JanetFunction Tests ===

public class JanetFunctionTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetFunctionTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Eval_Plus_Returns_Function_Type()
    {
        var result = _runtime.Eval("+");
        Assert.Equal(JanetType.Function, result.Type);
    }

    [Fact]
    public void AsFunction_On_Function_Works()
    {
        var result = _runtime.Eval("+");
        using var fn = result.AsFunction();
        Assert.Equal(JanetType.Function, fn.Type);
    }

    [Fact]
    public void AsFunction_On_NonFunction_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsFunction());
    }

    [Fact]
    public void Invoke_Plus_TwoNumbers()
    {
        using var fn = _runtime.GetFunction("+");
        var result = fn.Invoke(Janet.From(3.0), Janet.From(4.0));
        Assert.Equal(7.0, result.AsNumber());
    }

    [Fact]
    public void Invoke_Plus_MultipleNumbers()
    {
        using var fn = _runtime.GetFunction("+");
        var result = fn.Invoke(Janet.From(1.0), Janet.From(2.0), Janet.From(3.0), Janet.From(4.0));
        Assert.Equal(10.0, result.AsNumber());
    }

    [Fact]
    public void Invoke_Fn_Lambda()
    {
        // fn is a compiler special form, works under JANET_BOOTSTRAP
        var fnVal = _runtime.Eval("(fn [x] (+ x 1))");
        using var fn = fnVal.AsFunction();
        var result = fn.Invoke(Janet.From(5.0));
        Assert.Equal(6.0, result.AsNumber());
    }

    [Fact]
    public void Invoke_NoArgs()
    {
        var fnVal = _runtime.Eval("(fn [] 42)");
        using var fn = fnVal.AsFunction();
        var result = fn.Invoke();
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void Invoke_Error_Throws_JanetException()
    {
        using var fn = _runtime.GetFunction("error");
        var ex = Assert.Throws<JanetException>(() => fn.Invoke(Janet.From("boom")));
        Assert.Equal(JanetSignal.Error, ex.Signal);
    }

    [Fact]
    public void Invoke_NonThrowing_Returns_Signal()
    {
        using var fn = _runtime.GetFunction("error");
        var result = fn.Invoke([Janet.From("boom")], out var signal);
        Assert.Equal(JanetSignal.Error, signal);
    }

    [Fact]
    public void GetFunction_Convenience()
    {
        using var fn = _runtime.GetFunction("+");
        var result = fn.Invoke(Janet.From(10.0), Janet.From(20.0));
        Assert.Equal(30.0, result.AsNumber());
    }

    [Fact]
    public void Invoke_StringFunction()
    {
        // string/length is a C-level function available under JANET_BOOTSTRAP
        using var fn = _runtime.GetFunction("length");
        using var s = JanetString.Create("hello");
        var result = fn.Invoke(s.Value);
        Assert.Equal(5.0, result.AsNumber());
    }
}

// === JanetCallback Tests ===

public class JanetCallbackTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetCallbackTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Register_And_Call_Simple()
    {
        using var cb = _runtime.Register("add-one", args =>
            Janet.From(args[0].AsNumber() + 1));

        var result = _runtime.Eval("(add-one 5)");
        Assert.Equal(6.0, result.AsNumber());
    }

    [Fact]
    public void Register_And_Call_NoArgs()
    {
        using var cb = _runtime.Register("get-42", _ => Janet.From(42.0));

        var result = _runtime.Eval("(get-42)");
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void Register_And_Call_MultiArg()
    {
        using var cb = _runtime.Register("my-add", args =>
            Janet.From(args[0].AsNumber() + args[1].AsNumber()));

        var result = _runtime.Eval("(my-add 10 20)");
        Assert.Equal(30.0, result.AsNumber());
    }

    [Fact]
    public void Register_And_Call_String_Return()
    {
        using var cb = _runtime.Register("greet", args =>
            Janet.From("hello " + args[0].AsString()));

        var result = _runtime.Eval("(greet \"world\")");
        Assert.Equal("hello world", result.AsString());
    }

    [Fact]
    public void Callback_Exception_Becomes_Janet_Error()
    {
        using var cb = _runtime.Register("bad-fn", _ =>
            throw new InvalidOperationException("test error"));

        var result = _runtime.Eval("(bad-fn)", out var signal);
        Assert.Equal(JanetSignal.Error, signal);
    }

    [Fact]
    public void Callback_Dispose_Frees_Slot()
    {
        var cb = _runtime.Register("temp-fn", _ => Janet.From(1.0));
        var result = _runtime.Eval("(temp-fn)");
        Assert.Equal(1.0, result.AsNumber());

        cb.Dispose();
        // Slot is freed, can register another
        using var cb2 = _runtime.Register("temp-fn2", _ => Janet.From(2.0));
        result = _runtime.Eval("(temp-fn2)");
        Assert.Equal(2.0, result.AsNumber());
    }

    [Fact]
    public void Multiple_Callbacks_Coexist()
    {
        using var cb1 = _runtime.Register("fn-a", _ => Janet.From(1.0));
        using var cb2 = _runtime.Register("fn-b", _ => Janet.From(2.0));
        using var cb3 = _runtime.Register("fn-c", _ => Janet.From(3.0));

        Assert.Equal(1.0, _runtime.Eval("(fn-a)").AsNumber());
        Assert.Equal(2.0, _runtime.Eval("(fn-b)").AsNumber());
        Assert.Equal(3.0, _runtime.Eval("(fn-c)").AsNumber());
    }

    [Fact]
    public void Bidirectional_Call()
    {
        // C# registers callback, Janet function uses it, result returns to C#
        using var cb = _runtime.Register("double-it", args =>
            Janet.From(args[0].AsNumber() * 2));

        // Janet evaluates: call double-it on the result of (+ 1 2)
        var result = _runtime.Eval("(double-it (+ 1 2))");
        Assert.Equal(6.0, result.AsNumber());
    }

    [Fact]
    public void Callback_Called_From_Janet_Lambda()
    {
        // Register a callback, then call it from within a Janet lambda
        using var cb = _runtime.Register("square", args =>
            Janet.From(args[0].AsNumber() * args[0].AsNumber()));

        // Create a Janet function that uses the callback
        var fnVal = _runtime.Eval("(fn [x] (square x))");
        using var fn = fnVal.AsFunction();
        var result = fn.Invoke(Janet.From(7.0));
        Assert.Equal(49.0, result.AsNumber());
    }
}

// === JanetConvert Tests ===

public class JanetConvertTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetConvertTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void ToJanet_Double()
    {
        var j = JanetConvert.ToJanet(42.0);
        Assert.Equal(JanetType.Number, j.Type);
        Assert.Equal(42.0, j.AsNumber());
    }

    [Fact]
    public void ToJanet_Int()
    {
        var j = JanetConvert.ToJanet(7);
        Assert.Equal(JanetType.Number, j.Type);
        Assert.Equal(7, j.AsInteger());
    }

    [Fact]
    public void ToJanet_Bool_True()
    {
        var j = JanetConvert.ToJanet(true);
        Assert.Equal(JanetType.Boolean, j.Type);
        Assert.True(j.AsBoolean());
    }

    [Fact]
    public void ToJanet_Bool_False()
    {
        var j = JanetConvert.ToJanet(false);
        Assert.Equal(JanetType.Boolean, j.Type);
        Assert.False(j.AsBoolean());
    }

    [Fact]
    public void ToJanet_String()
    {
        var j = JanetConvert.ToJanet("hello");
        Assert.Equal(JanetType.String, j.Type);
        Assert.Equal("hello", j.AsString());
    }

    [Fact]
    public void ToJanet_Null_Returns_Nil()
    {
        var j = JanetConvert.ToJanet(null);
        Assert.True(j.IsNil);
    }

    [Fact]
    public void ToJanet_Janet_Passthrough()
    {
        var original = Janet.From(99.0);
        var j = JanetConvert.ToJanet(original);
        Assert.Equal(99.0, j.AsNumber());
    }

    [Fact]
    public void ToClr_Number_To_Double()
    {
        var j = Janet.From(3.14);
        Assert.Equal(3.14, JanetConvert.ToClr<double>(j));
    }

    [Fact]
    public void ToClr_Number_To_Int()
    {
        var j = Janet.From(42);
        Assert.Equal(42, JanetConvert.ToClr<int>(j));
    }

    [Fact]
    public void ToClr_Boolean_To_Bool()
    {
        Assert.True(JanetConvert.ToClr<bool>(Janet.True));
        Assert.False(JanetConvert.ToClr<bool>(Janet.False));
    }

    [Fact]
    public void ToClr_String_To_String()
    {
        var j = Janet.From("test");
        Assert.Equal("test", JanetConvert.ToClr<string>(j));
    }

    [Fact]
    public void ToClr_Nil_To_Nullable_Returns_Null()
    {
        Assert.Null(JanetConvert.ToClr<string>(Janet.Nil));
    }

    [Fact]
    public void ToClr_Nil_To_NonNullable_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => JanetConvert.ToClr<int>(Janet.Nil));
    }

    [Fact]
    public void Roundtrip_Double()
    {
        var j = JanetConvert.ToJanet(2.718);
        Assert.Equal(2.718, JanetConvert.ToClr<double>(j));
    }

    [Fact]
    public void Roundtrip_String()
    {
        var j = JanetConvert.ToJanet("roundtrip");
        Assert.Equal("roundtrip", JanetConvert.ToClr<string>(j));
    }

    [Fact]
    public void Roundtrip_Bool()
    {
        var j = JanetConvert.ToJanet(true);
        Assert.True(JanetConvert.ToClr<bool>(j));
    }
}
