using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Fiber (Coroutine) Tests ===

public class FiberTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public FiberTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void CreateFiber_StatusIsNew()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);
        Assert.Equal(JanetFiberStatus.New, fiber.Status);
    }

    [Fact]
    public void CreateFiber_CanResume_IsTrue()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);
        Assert.True(fiber.CanResume);
    }

    [Fact]
    public void Resume_SimpleFunction_ReturnsValue()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);

        var result = fiber.Resume();
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void Resume_SimpleFunction_SignalIsOk()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);

        var result = fiber.Resume(Janet.Nil, out var signal);
        Assert.Equal(JanetSignal.Ok, signal);
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void Resume_AfterCompletion_StatusIsDead()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);

        fiber.Resume();
        Assert.Equal(JanetFiberStatus.Dead, fiber.Status);
        Assert.False(fiber.CanResume);
    }

    [Fact]
    public void Resume_WithYield_YieldsValue()
    {
        using var fn = _runtime.GetFunction("(fn [] (yield 10) 20)");
        using var fiber = JanetFiber.Create(fn);

        // First resume: hits yield, returns 10
        var result1 = fiber.Resume(Janet.Nil, out var signal1);
        Assert.Equal(JanetSignal.Yield, signal1);
        Assert.Equal(10.0, result1.AsNumber());
        Assert.Equal(JanetFiberStatus.Pending, fiber.Status);
        Assert.True(fiber.CanResume);

        // Second resume: function returns 20
        var result2 = fiber.Resume(Janet.Nil, out var signal2);
        Assert.Equal(JanetSignal.Ok, signal2);
        Assert.Equal(20.0, result2.AsNumber());
        Assert.Equal(JanetFiberStatus.Dead, fiber.Status);
        Assert.False(fiber.CanResume);
    }

    [Fact]
    public void Resume_MultipleYields()
    {
        using var fn = _runtime.GetFunction("(fn [] (yield 1) (yield 2) (yield 3) 4)");
        using var fiber = JanetFiber.Create(fn);

        for (int i = 1; i <= 3; i++)
        {
            var result = fiber.Resume(Janet.Nil, out var signal);
            Assert.Equal(JanetSignal.Yield, signal);
            Assert.Equal((double)i, result.AsNumber());
            Assert.True(fiber.CanResume);
        }

        // Final resume returns 4
        var final = fiber.Resume(Janet.Nil, out var finalSignal);
        Assert.Equal(JanetSignal.Ok, finalSignal);
        Assert.Equal(4.0, final.AsNumber());
        Assert.False(fiber.CanResume);
    }

    [Fact]
    public void Resume_DeadFiber_ReturnsError()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);

        fiber.Resume(); // completes the fiber

        // Resuming a dead fiber should error
        var result = fiber.Resume(Janet.Nil, out var signal);
        Assert.Equal(JanetSignal.Error, signal);
    }

    [Fact]
    public void Resume_DeadFiber_ThrowingVariant_Throws()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);

        fiber.Resume(); // completes the fiber

        Assert.Throws<JanetException>(() => fiber.Resume());
    }

    [Fact]
    public void Resume_FiberThatErrors_ReturnsErrorSignal()
    {
        using var fn = _runtime.GetFunction("(fn [] (error \"oops\"))");
        using var fiber = JanetFiber.Create(fn);

        var result = fiber.Resume(Janet.Nil, out var signal);
        Assert.Equal(JanetSignal.Error, signal);
        Assert.Equal("oops", result.AsString());
    }

    [Fact]
    public void Resume_FiberThatErrors_ThrowingVariant_ThrowsJanetException()
    {
        using var fn = _runtime.GetFunction("(fn [] (error \"oops\"))");
        using var fiber = JanetFiber.Create(fn);

        var ex = Assert.Throws<JanetException>(() => fiber.Resume());
        Assert.Equal(JanetSignal.Error, ex.Signal);
        Assert.Equal("oops", ex.ErrorValue.AsString());
    }

    [Fact]
    public void Fiber_WithArguments()
    {
        using var fn = _runtime.GetFunction("(fn [x y] (+ x y))");
        using var fiber = JanetFiber.Create(fn, Janet.From(10.0), Janet.From(32.0));

        var result = fiber.Resume();
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void Fiber_YieldReceivesResumeValue()
    {
        // yield returns the value passed to the next resume
        using var fn = _runtime.GetFunction("(fn [] (+ 1 (yield 0)))");
        using var fiber = JanetFiber.Create(fn);

        // First resume: yields 0
        var result1 = fiber.Resume(Janet.Nil, out var signal1);
        Assert.Equal(JanetSignal.Yield, signal1);
        Assert.Equal(0.0, result1.AsNumber());

        // Second resume with value 99: yield returns 99, function returns 1+99=100
        var result2 = fiber.Resume(Janet.From(99.0), out var signal2);
        Assert.Equal(JanetSignal.Ok, signal2);
        Assert.Equal(100.0, result2.AsNumber());
    }

    [Fact]
    public void AsFiber_OnFiberValue_Succeeds()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);

        // Get the raw Janet value and wrap it back via AsFiber
        var janetVal = fiber.Value;
        Assert.Equal(JanetType.Fiber, janetVal.Type);

        using var fiber2 = janetVal.AsFiber();
        Assert.Equal(JanetFiberStatus.New, fiber2.Status);
    }

    [Fact]
    public void AsFiber_OnNonFiber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsFiber());
    }

    [Fact]
    public void AsFiber_OnString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From("hello").AsFiber());
    }

    [Fact]
    public void Fiber_DoubleDispose_DoesNotCrash()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        var fiber = JanetFiber.Create(fn);
        fiber.Dispose();
        fiber.Dispose(); // should not throw or crash
    }

    [Fact]
    public void Fiber_AccessAfterDispose_Throws()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        var fiber = JanetFiber.Create(fn);
        fiber.Dispose();
        Assert.Throws<ObjectDisposedException>(() => fiber.Status);
    }

    [Fact]
    public void Fiber_ResumeAfterDispose_Throws()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        var fiber = JanetFiber.Create(fn);
        fiber.Dispose();
        Assert.Throws<ObjectDisposedException>(() => fiber.Resume());
    }

    [Fact]
    public void Fiber_CanResumeAfterDispose_Throws()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        var fiber = JanetFiber.Create(fn);
        fiber.Dispose();
        Assert.Throws<ObjectDisposedException>(() => fiber.CanResume);
    }

    [Fact]
    public void Fiber_TypeIsFiber()
    {
        using var fn = _runtime.GetFunction("(fn [] 42)");
        using var fiber = JanetFiber.Create(fn);
        Assert.Equal(JanetType.Fiber, fiber.Type);
    }
}
