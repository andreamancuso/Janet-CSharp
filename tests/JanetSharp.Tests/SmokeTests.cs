using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

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
