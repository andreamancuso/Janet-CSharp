using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Janet Value Type Tests (the C# Janet struct) ===

public class JanetValueTypeTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetValueTypeTests()
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
    public void ImplicitOperator_JanetValue_To_Janet_Works()
    {
        using var table = JanetTable.Create();
        // Implicitly converts JanetTable (JanetValue) to Janet struct
        Janet raw = table; 
        Assert.Equal(JanetType.Table, raw.Type);
    }

    [Fact]
    public void AsTable_OnTableValue_ReturnsWrapper()
    {
        var raw = _runtime.Eval("@{}");
        using var table = raw.AsTable();
        Assert.NotNull(table);
    }

    [Fact]
    public void AsArray_OnArrayValue_ReturnsWrapper()
    {
        var raw = _runtime.Eval("@[]");
        using var array = raw.AsArray();
        Assert.NotNull(array);
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
