using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Smoke Tests ===

public class SmokeTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public SmokeTests()
    {
        _runtime = new JanetRuntime();
    }

    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Init_and_Deinit_DoNotCrash()
    {
        // If we get here, init succeeded (in ctor).
        // Deinit happens in Dispose. No crash = pass.
        Assert.True(true);
    }

    [Fact]
    public void WrapNumber_Roundtrip()
    {
        long wrapped = NativeMethods.shim_wrap_number(42.0);
        int type = NativeMethods.shim_type(wrapped);
        double unwrapped = NativeMethods.shim_unwrap_number(wrapped);

        Assert.Equal(0, type); // JANET_NUMBER = 0
        Assert.Equal(42.0, unwrapped);
    }

    [Fact]
    public void WrapNil_HasCorrectType()
    {
        long nil = NativeMethods.shim_wrap_nil();
        int type = NativeMethods.shim_type(nil);

        Assert.Equal(1, type); // JANET_NIL = 1
    }

    [Fact]
    public void WrapBoolean_Roundtrip()
    {
        long trueVal = NativeMethods.shim_wrap_boolean(1);
        long falseVal = NativeMethods.shim_wrap_boolean(0);

        Assert.Equal(2, NativeMethods.shim_type(trueVal));  // JANET_BOOLEAN = 2
        Assert.Equal(2, NativeMethods.shim_type(falseVal));
        Assert.Equal(1, NativeMethods.shim_unwrap_boolean(trueVal));
        Assert.Equal(0, NativeMethods.shim_unwrap_boolean(falseVal));
    }

    [Fact]
    public void CoreEnv_ReturnsNonNull()
    {
        Assert.NotEqual(IntPtr.Zero, _runtime.CoreEnvironment);
    }

    [Fact]
    public void DoString_EvaluatesExpression()
    {
        var result = _runtime.Eval("(+ 1 2)");
        Assert.Equal(3.0, result.AsNumber());
    }

    [Fact]
    public void DoString_WithError_ReturnsErrorSignal()
    {
        _runtime.Eval("(error \"test error\")", out var signal);
        Assert.Equal(JanetSignal.Error, signal);
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
