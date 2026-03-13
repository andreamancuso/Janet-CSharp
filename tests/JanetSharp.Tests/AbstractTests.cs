using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Custom Abstract Type Tests ===

public class AbstractTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public AbstractTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_TypeIsAbstract()
    {
        using var abs = JanetAbstract.Create("hello");
        Assert.Equal(JanetType.Abstract, abs.Type);
    }

    [Fact]
    public void Create_NullTarget_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JanetAbstract.Create(null!));
    }

    [Fact]
    public void Target_ReturnsSameReference()
    {
        var obj = new object();
        using var abs = JanetAbstract.Create(obj);
        Assert.Same(obj, abs.Target);
    }

    [Fact]
    public void Target_StringValue()
    {
        using var abs = JanetAbstract.Create("test-string");
        Assert.Equal("test-string", abs.Target);
    }

    [Fact]
    public void GetTargetT_CastsCorrectly()
    {
        var list = new List<int> { 1, 2, 3 };
        using var abs = JanetAbstract.Create(list);
        var result = abs.GetTarget<List<int>>();
        Assert.Same(list, result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetTargetT_WrongType_ThrowsInvalidCastException()
    {
        using var abs = JanetAbstract.Create("a string");
        Assert.Throws<InvalidCastException>(() => abs.GetTarget<List<int>>());
    }

    [Fact]
    public void StaticGetTarget_ExtractsObject()
    {
        var obj = new object();
        using var abs = JanetAbstract.Create(obj);
        var extracted = JanetAbstract.GetTarget(abs.Value);
        Assert.Same(obj, extracted);
    }

    [Fact]
    public void StaticGetTargetT_ExtractsTypedObject()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };
        using var abs = JanetAbstract.Create(dict);
        var extracted = JanetAbstract.GetTarget<Dictionary<string, int>>(abs.Value);
        Assert.Same(dict, extracted);
        Assert.Equal(1, extracted["a"]);
    }

    [Fact]
    public void StaticGetTarget_OnNumber_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => JanetAbstract.GetTarget(Janet.From(42.0)));
    }

    [Fact]
    public void StaticGetTarget_OnNil_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => JanetAbstract.GetTarget(Janet.Nil));
    }

    [Fact]
    public void StaticGetTarget_OnString_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => JanetAbstract.GetTarget(Janet.From("hello")));
    }

    [Fact]
    public void ShimAbstractCheck_TrueForOurAbstract()
    {
        using var abs = JanetAbstract.Create("test");
        Assert.Equal(1, NativeMethods.shim_abstract_check(abs.Value.RawValue));
    }

    [Fact]
    public void ShimAbstractCheck_FalseForNumber()
    {
        Assert.Equal(0, NativeMethods.shim_abstract_check(Janet.From(42.0).RawValue));
    }

    [Fact]
    public void ShimAbstractCheck_FalseForNil()
    {
        Assert.Equal(0, NativeMethods.shim_abstract_check(Janet.Nil.RawValue));
    }

    [Fact]
    public void ShimAbstractCheck_FalseForString()
    {
        Assert.Equal(0, NativeMethods.shim_abstract_check(Janet.From("hello").RawValue));
    }

    [Fact]
    public void AsAbstract_OnAbstractValue_Succeeds()
    {
        using var abs = JanetAbstract.Create("test");
        var janetVal = abs.Value;
        using var abs2 = janetVal.AsAbstract();
        Assert.Equal("test", abs2.Target);
    }

    [Fact]
    public void AsAbstract_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsAbstract());
    }

    [Fact]
    public void AsAbstract_OnString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From("hello").AsAbstract());
    }

    [Fact]
    public void RoundTrip_ThroughJanetEnvironment()
    {
        var obj = new List<string> { "round", "trip" };
        using var abs = JanetAbstract.Create(obj);

        // Register abstract value in Janet environment
        NativeMethods.shim_def(_runtime.CoreEnvironment, "my-obj", abs.Value.RawValue);

        // Retrieve it back via eval
        var result = _runtime.Eval("my-obj");

        // Extract and verify identity
        var extracted = JanetAbstract.GetTarget<List<string>>(result);
        Assert.Same(obj, extracted);
        Assert.Equal("round", extracted[0]);
    }

    [Fact]
    public void DoubleDispose_DoesNotCrash()
    {
        var abs = JanetAbstract.Create("test");
        abs.Dispose();
        abs.Dispose(); // should not throw
    }

    [Fact]
    public void AccessAfterDispose_Throws()
    {
        var abs = JanetAbstract.Create("test");
        abs.Dispose();
        Assert.Throws<ObjectDisposedException>(() => abs.Target);
    }

    [Fact]
    public void MultipleAbstracts_DifferentTypes()
    {
        using var abs1 = JanetAbstract.Create("a string");
        using var abs2 = JanetAbstract.Create(42);
        using var abs3 = JanetAbstract.Create(new List<double> { 1.0, 2.0 });

        Assert.Equal("a string", abs1.GetTarget<string>());
        Assert.Equal(42, abs2.GetTarget<int>());
        Assert.Equal(2, abs3.GetTarget<List<double>>().Count);
    }

    [Fact]
    public void AbstractStoredInJanetArray_Survives()
    {
        var obj = new object();
        using var abs = JanetAbstract.Create(obj);
        using var arr = JanetArray.Create();
        arr.Add(abs.Value);

        var retrieved = arr[0];
        Assert.Equal(JanetType.Abstract, retrieved.Type);
        var extracted = JanetAbstract.GetTarget(retrieved);
        Assert.Same(obj, extracted);
    }

    [Fact]
    public void AbstractStoredInJanetTable_Survives()
    {
        var obj = new object();
        using var abs = JanetAbstract.Create(obj);
        using var tbl = JanetTable.Create();
        tbl[Janet.From("key")] = abs.Value;

        var retrieved = tbl[Janet.From("key")];
        Assert.Equal(JanetType.Abstract, retrieved.Type);
        var extracted = JanetAbstract.GetTarget(retrieved);
        Assert.Same(obj, extracted);
    }

    [Fact]
    public void AbstractInCallback_RoundTrip()
    {
        // Register a callback that receives an abstract and extracts its target
        using var cb = new JanetCallback(args =>
        {
            var target = JanetAbstract.GetTarget<string>(args[0]);
            return Janet.From(target + "-processed");
        });
        NativeMethods.shim_def(_runtime.CoreEnvironment, "process-obj", cb.Value.RawValue);

        // Create abstract and pass it to the callback
        using var abs = JanetAbstract.Create("input");
        NativeMethods.shim_def(_runtime.CoreEnvironment, "my-abs", abs.Value.RawValue);

        var result = _runtime.Eval("(process-obj my-abs)");
        Assert.Equal("input-processed", result.AsString());
    }
}
