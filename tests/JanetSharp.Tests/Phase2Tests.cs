using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Janet Struct Tests ===

public class JanetStructTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetStructTests()
    {
        _runtime = new JanetRuntime();
    }

    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void From_Double_Roundtrips()
    {
        var j = Janet.From(42.0);
        Assert.Equal(JanetType.Number, j.Type);
        Assert.Equal(42.0, j.AsNumber());
    }

    [Fact]
    public void From_Int_Roundtrips()
    {
        var j = Janet.From(7);
        Assert.Equal(JanetType.Number, j.Type);
        Assert.Equal(7, j.AsInteger());
    }

    [Fact]
    public void Nil_HasCorrectType()
    {
        var j = Janet.Nil;
        Assert.Equal(JanetType.Nil, j.Type);
        Assert.True(j.IsNil);
        Assert.False(j.IsTruthy);
    }

    [Fact]
    public void Boolean_Roundtrips()
    {
        Assert.True(Janet.True.AsBoolean());
        Assert.False(Janet.False.AsBoolean());
        Assert.Equal(JanetType.Boolean, Janet.True.Type);
        Assert.True(Janet.True.IsTruthy);
        Assert.False(Janet.False.IsTruthy);
    }

    [Fact]
    public void From_Bool_Roundtrips()
    {
        Assert.True(Janet.From(true).AsBoolean());
        Assert.False(Janet.From(false).AsBoolean());
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = Janet.From(42.0);
        var b = Janet.From(42.0);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = Janet.From(1.0);
        var b = Janet.From(2.0);
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void AsNumber_OnNil_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.Nil.AsNumber());
    }

    [Fact]
    public void IsGcType_PrimitivesAreFalse()
    {
        Assert.False(Janet.From(1.0).IsGcType);
        Assert.False(Janet.Nil.IsGcType);
        Assert.False(Janet.True.IsGcType);
    }

    [Fact]
    public void ToString_ShowsType()
    {
        Assert.Equal("Janet(Number)", Janet.From(1.0).ToString());
        Assert.Equal("Janet(Nil)", Janet.Nil.ToString());
    }
}

// === JanetValue Smart Pointer Tests ===

public class JanetValueTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetValueTests()
    {
        _runtime = new JanetRuntime();
    }

    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Wrap_Primitive_DoesNotCrash()
    {
        using var val = new JanetValue(Janet.From(42.0));
        Assert.Equal(JanetType.Number, val.Type);
        Assert.Equal(42.0, val.Value.AsNumber());
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var val = new JanetValue(Janet.From(1.0));
        val.Dispose();
        val.Dispose(); // second dispose should not crash
    }

    [Fact]
    public void Value_AfterDispose_Throws()
    {
        var val = new JanetValue(Janet.From(1.0));
        val.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = val.Value);
    }
}

// === JanetRuntime Tests ===

public class JanetRuntimeTests
{
    [Fact]
    public void Init_And_Dispose_Work()
    {
        using var runtime = new JanetRuntime();
        Assert.NotEqual(IntPtr.Zero, runtime.CoreEnvironment);
    }

    [Fact]
    public void Eval_ReturnsResult()
    {
        using var runtime = new JanetRuntime();
        var result = runtime.Eval("(+ 1 2)");
        Assert.Equal(JanetType.Number, result.Type);
        Assert.Equal(3.0, result.AsNumber());
    }

    [Fact]
    public void Eval_WithError_Throws()
    {
        using var runtime = new JanetRuntime();
        var ex = Assert.Throws<JanetException>(() => runtime.Eval("(error \"boom\")"));
        Assert.Equal(JanetSignal.Error, ex.Signal);
    }

    [Fact]
    public void Eval_WithSignalOut_DoesNotThrow()
    {
        using var runtime = new JanetRuntime();
        var result = runtime.Eval("(error \"boom\")", out var signal);
        Assert.Equal(JanetSignal.Error, signal);
    }

    [Fact]
    public void DoubleInit_Throws()
    {
        using var runtime1 = new JanetRuntime();
        Assert.Throws<InvalidOperationException>(() => new JanetRuntime());
    }

    [Fact]
    public void AfterDispose_CanCreateNew()
    {
        var runtime1 = new JanetRuntime();
        runtime1.Dispose();

        using var runtime2 = new JanetRuntime();
        var result = runtime2.Eval("(+ 10 20)");
        Assert.Equal(30.0, result.AsNumber());
    }

    [Fact]
    public void Eval_AfterDispose_Throws()
    {
        var runtime = new JanetRuntime();
        runtime.Dispose();
        Assert.Throws<ObjectDisposedException>(() => runtime.Eval("1"));
    }
}
