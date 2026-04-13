using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Dispose & Lifecycle Safety ===

public class DisposeSafetyTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public DisposeSafetyTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void JanetString_DoubleDispose_DoesNotCrash()
    {
        var s = JanetString.Create("hello");
        s.Dispose();
        s.Dispose(); // should not throw or crash
    }

    [Fact]
    public void JanetSymbol_DoubleDispose_DoesNotCrash()
    {
        var s = JanetSymbol.Create("mysym");
        s.Dispose();
        s.Dispose();
    }

    [Fact]
    public void JanetKeyword_DoubleDispose_DoesNotCrash()
    {
        var k = JanetKeyword.Create("mykey");
        k.Dispose();
        k.Dispose();
    }

    [Fact]
    public void JanetArray_DoubleDispose_DoesNotCrash()
    {
        var a = JanetArray.Create();
        a.Dispose();
        a.Dispose();
    }

    [Fact]
    public void JanetTuple_DoubleDispose_DoesNotCrash()
    {
        var t = JanetTuple.Create(Janet.From(1.0));
        t.Dispose();
        t.Dispose();
    }

    [Fact]
    public void JanetTable_DoubleDispose_DoesNotCrash()
    {
        var t = JanetTable.Create();
        t.Dispose();
        t.Dispose();
    }

    [Fact]
    public void JanetStruct_DoubleDispose_DoesNotCrash()
    {
        var val = _runtime.Eval("{}");
        using var s = val.AsStruct();
        s.Dispose();
        s.Dispose();
    }

    [Fact]
    public void JanetBuffer_DoubleDispose_DoesNotCrash()
    {
        var b = JanetBuffer.Create();
        b.Dispose();
        b.Dispose();
    }

    [Fact]
    public void JanetFunction_DoubleDispose_DoesNotCrash()
    {
        var fn = _runtime.GetFunction("+");
        fn.Dispose();
        fn.Dispose();
    }

    [Fact]
    public void JanetCallback_DoubleDispose_DoesNotCrash()
    {
        var cb = new JanetCallback(_ => Janet.Nil);
        cb.Dispose();
        cb.Dispose();
    }

    [Fact]
    public void JanetString_AccessAfterDispose_Throws()
    {
        var s = JanetString.Create("hello");
        s.Dispose();
        Assert.Throws<ObjectDisposedException>(() => s.Length);
    }

    [Fact]
    public void JanetArray_AccessAfterDispose_Throws()
    {
        var a = JanetArray.Create();
        a.Add(Janet.From(1.0));
        a.Dispose();
        Assert.Throws<ObjectDisposedException>(() => a.Count);
    }

    [Fact]
    public void JanetTable_AccessAfterDispose_Throws()
    {
        var t = JanetTable.Create();
        t.Dispose();
        Assert.Throws<ObjectDisposedException>(() => t.Count);
    }

    [Fact]
    public void JanetBuffer_AccessAfterDispose_Throws()
    {
        var b = JanetBuffer.Create();
        b.Dispose();
        Assert.Throws<ObjectDisposedException>(() => b.Count);
    }

    [Fact]
    public void JanetFunction_InvokeAfterDispose_Throws()
    {
        var fn = _runtime.GetFunction("+");
        fn.Dispose();
        Assert.Throws<ObjectDisposedException>(() => fn.Invoke(Janet.From(1.0), Janet.From(2.0)));
    }
    [Fact]
    public void Finalizer_DeferredUnroot_ProcessedOnEval()
    {
        // 1. Create a rooted value in an inner scope so the reference is lost
        void CreateLostValue()
        {
            var table = JanetTable.Create();
            table["key"] = Janet.From("value");
        }

        CreateLostValue();

        // 2. Force GC to run the finalizer (which queues the deferred unroot)
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // 3. Trigger an evaluation to process the deferred unroots
        // This should run ProcessDeferredUnroots() and NOT crash the thread.
        var result = _runtime.Eval("nil");
        Assert.True(result.IsNil);
    }
}

// === Invalid Type Conversion Tests ===

public class TypeConversionErrorTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public TypeConversionErrorTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    // --- AsNumber ---

    [Fact]
    public void AsNumber_OnNil_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.Nil.AsNumber());
    }

    [Fact]
    public void AsNumber_OnBoolean_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.True.AsNumber());
    }

    [Fact]
    public void AsNumber_OnString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From("hello").AsNumber());
    }

    // --- AsBoolean ---

    [Fact]
    public void AsBoolean_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsBoolean());
    }

    [Fact]
    public void AsBoolean_OnNil_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.Nil.AsBoolean());
    }

    [Fact]
    public void AsBoolean_OnString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From("true").AsBoolean());
    }

    // --- AsInteger ---

    [Fact]
    public void AsInteger_OnNil_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.Nil.AsInteger());
    }

    [Fact]
    public void AsInteger_OnBoolean_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.True.AsInteger());
    }

    // --- AsString ---

    [Fact]
    public void AsString_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsString());
    }

    [Fact]
    public void AsString_OnBoolean_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.True.AsString());
    }

    [Fact]
    public void AsString_OnNil_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.Nil.AsString());
    }

    // --- AsArray on wrong types ---

    [Fact]
    public void AsArray_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsArray());
    }

    [Fact]
    public void AsArray_OnString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From("hello").AsArray());
    }

    // --- AsTuple on wrong types ---

    [Fact]
    public void AsTuple_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsTuple());
    }

    // --- AsTable on wrong types ---

    [Fact]
    public void AsTable_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsTable());
    }

    // --- AsStruct on wrong types ---

    [Fact]
    public void AsStruct_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsStruct());
    }

    // --- AsBuffer on wrong types ---

    [Fact]
    public void AsBuffer_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsBuffer());
    }

    // --- AsFunction on wrong types ---

    [Fact]
    public void AsFunction_OnNumber_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From(42.0).AsFunction());
    }

    [Fact]
    public void AsFunction_OnString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Janet.From("hello").AsFunction());
    }

    // --- JanetConvert unsupported types ---

    [Fact]
    public void ToJanet_Decimal_Throws()
    {
        Assert.Throws<ArgumentException>(() => JanetConvert.ToJanet(42m));
    }

    [Fact]
    public void ToJanet_Guid_Throws()
    {
        Assert.Throws<ArgumentException>(() => JanetConvert.ToJanet(Guid.NewGuid()));
    }

    [Fact]
    public void ToJanet_List_Throws()
    {
        Assert.Throws<ArgumentException>(() => JanetConvert.ToJanet(new List<int> { 1, 2 }));
    }

    [Fact]
    public void ToClr_Decimal_Throws()
    {
        Assert.Throws<ArgumentException>(() => JanetConvert.ToClr<decimal>(Janet.From(42.0)));
    }

    [Fact]
    public void ToClr_Guid_Throws()
    {
        Assert.Throws<ArgumentException>(() => JanetConvert.ToClr<Guid>(Janet.From("not-a-guid")));
    }
}

// === Infinite Callback Support Tests ===

public class CallbackSlotTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public CallbackSlotTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_LargeNumberOfCallbacks_Succeeds()
    {
        const int count = 1000;
        var callbacks = new JanetCallback[count];
        try
        {
            for (int i = 0; i < count; i++)
            {
                int captured = i;
                callbacks[i] = new JanetCallback(_ => Janet.From((double)captured));
            }

            // All slots allocated successfully
            Assert.NotNull(callbacks[count - 1]);

            // Test the last one just to be sure
            NativeMethods.shim_def(_runtime.CoreEnvironment, "test-cb", callbacks[count - 1].Value.RawValue);
            var result = _runtime.Eval("(test-cb)");
            Assert.Equal((double)(count - 1), result.AsNumber());
        }
        finally
        {
            foreach (var cb in callbacks)
                cb?.Dispose();
        }
    }
}

// === Thread Affinity Enforcement ===

public class ThreadAffinityTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public ThreadAffinityTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Eval_FromWrongThread_Throws()
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try { _runtime.Eval("(+ 1 2)"); }
            catch (Exception ex) { caught = ex; }
        });
        thread.Start();
        thread.Join();

        var ioe = Assert.IsType<InvalidOperationException>(caught);
        Assert.Contains("thread", ioe.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_FromWrongThread_Throws()
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try { _runtime.Register("bad", _ => Janet.Nil); }
            catch (Exception ex) { caught = ex; }
        });
        thread.Start();
        thread.Join();

        var ioe = Assert.IsType<InvalidOperationException>(caught);
        Assert.Contains("thread", ioe.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RuntimeCreation_AfterDispose_OnNewThread_Succeeds()
    {
        _runtime.Dispose();

        // The singleton should be freed; creating on the same thread should work
        using var runtime2 = new JanetRuntime();
        var result = runtime2.Eval("(+ 1 1)");
        Assert.Equal(2.0, result.AsNumber());
    }
}

// === Edge Case Tests ===

public class EdgeCaseTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public EdgeCaseTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    // --- Empty collection operations ---

    [Fact]
    public void Clear_EmptyArray_DoesNotCrash()
    {
        using var arr = JanetArray.Create();
        arr.Clear(); // should not throw
        Assert.Equal(0, arr.Count);
    }

    [Fact]
    public void Clear_EmptyTable_DoesNotCrash()
    {
        using var tbl = JanetTable.Create();
        tbl.Clear();
        Assert.Equal(0, tbl.Count);
    }

    [Fact]
    public void Remove_FromEmptyTable_ReturnsFalse()
    {
        using var tbl = JanetTable.Create();
        Assert.False(tbl.Remove(Janet.From("key")));
    }

    // --- Nil in collections ---

    [Fact]
    public void Array_CanContainNil()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.Nil);
        arr.Add(Janet.From(1.0));
        arr.Add(Janet.Nil);

        Assert.Equal(3, arr.Count);
        Assert.True(arr[0].IsNil);
        Assert.Equal(1.0, arr[1].AsNumber());
        Assert.True(arr[2].IsNil);
    }

    // --- Large argument lists ---

    [Fact]
    public void Invoke_With20Args()
    {
        using var fn = _runtime.GetFunction("+");
        var args = new Janet[20];
        for (int i = 0; i < 20; i++)
            args[i] = Janet.From(1.0);

        var result = fn.Invoke(args);
        Assert.Equal(20.0, result.AsNumber());
    }

    // --- Janet.Equals / GetHashCode ---

    [Fact]
    public void Equal_Numbers_HaveSameHashCode()
    {
        var a = Janet.From(42.0);
        var b = Janet.From(42.0);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Janet_In_Dictionary()
    {
        var dict = new Dictionary<Janet, string>
        {
            [Janet.From(1.0)] = "one",
            [Janet.From(2.0)] = "two"
        };

        Assert.Equal("one", dict[Janet.From(1.0)]);
        Assert.Equal("two", dict[Janet.From(2.0)]);
        Assert.False(dict.ContainsKey(Janet.From(3.0)));
    }

    [Fact]
    public void Janet_Equals_Null_ReturnsFalse()
    {
        var j = Janet.From(42.0);
        object? nullObj = null;
        bool result = j.Equals(nullObj);
        Assert.False(result);
    }

    [Fact]
    public void Janet_Equals_NonJanet_ReturnsFalse()
    {
        var j = Janet.From(42.0);
        Assert.False(j.Equals("not a janet"));
    }

    [Fact]
    public void Janet_ToString_FormatByType()
    {
        Assert.Equal("Janet(Number)", Janet.From(42.0).ToString());
        Assert.Equal("Janet(Nil)", Janet.Nil.ToString());
        Assert.Equal("Janet(Boolean)", Janet.True.ToString());
        Assert.Equal("Janet(String)", Janet.From("hi").ToString());
    }

    [Fact]
    public void Janet_Nil_IsTruthy_IsFalse()
    {
        Assert.False(Janet.Nil.IsTruthy);
    }

    [Fact]
    public void Janet_False_IsTruthy_IsFalse()
    {
        Assert.False(Janet.False.IsTruthy);
    }

    [Fact]
    public void Janet_True_IsTruthy_IsTrue()
    {
        Assert.True(Janet.True.IsTruthy);
    }

    [Fact]
    public void Janet_Number_IsTruthy_IsTrue()
    {
        Assert.True(Janet.From(0.0).IsTruthy); // In Janet, 0 is truthy
    }

    [Fact]
    public void Janet_String_IsTruthy_IsTrue()
    {
        Assert.True(Janet.From("").IsTruthy); // In Janet, empty string is truthy
    }

    // --- JanetException ---

    [Fact]
    public void JanetException_Captures_ErrorValue_And_Signal()
    {
        using var fn = _runtime.GetFunction("error");
        var ex = Assert.Throws<JanetException>(() => fn.Invoke(Janet.From("test error")));

        Assert.Equal(JanetSignal.Error, ex.Signal);
        Assert.Equal(JanetType.String, ex.ErrorValue.Type);
    }

    [Fact]
    public void JanetException_Message_Contains_Signal()
    {
        var ex = new JanetException(Janet.Nil, JanetSignal.Error);
        Assert.Contains("Error", ex.Message);
    }

    [Fact]
    public void JanetException_StringConstructor()
    {
        var ex = new JanetException("custom message");
        Assert.Equal("custom message", ex.Message);
    }

    [Fact]
    public void JanetException_InnerExceptionConstructor()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new JanetException("outer", inner);
        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    // --- UTF-8 edge cases ---

    [Fact]
    public void JanetString_ThreeByte_ChineseCharacters()
    {
        using var s = JanetString.Create("\u4f60\u597d"); // 你好
        Assert.Equal("\u4f60\u597d", s.ToString());
        Assert.Equal(6, s.Length); // 3 bytes per character
    }

    [Fact]
    public void JanetString_FourByte_Emoji()
    {
        using var s = JanetString.Create("\U0001F600"); // 😀
        Assert.Equal("\U0001F600", s.ToString());
        Assert.Equal(4, s.Length); // 4 bytes for this emoji
    }

    [Fact]
    public void JanetString_MixedAsciiAndUtf8()
    {
        using var s = JanetString.Create("hello \u00e9 \u4f60\u597d \U0001F600");
        var str = s.ToString();
        Assert.Equal("hello \u00e9 \u4f60\u597d \U0001F600", str);
    }

    // --- Callback exception types ---

    [Fact]
    public void Callback_NullReferenceException_Becomes_JanetError()
    {
        using var cb = _runtime.Register("null-ref-fn", _ =>
        {
            string? s = null;
            return Janet.From(s!.Length); // NullReferenceException
        });

        var result = _runtime.Eval("(null-ref-fn)", out var signal);
        Assert.Equal(JanetSignal.Error, signal);
    }

    [Fact]
    public void Callback_ArgumentException_Becomes_JanetError()
    {
        using var cb = _runtime.Register("arg-ex-fn", _ =>
            throw new ArgumentException("bad arg"));

        var result = _runtime.Eval("(arg-ex-fn)", out var signal);
        Assert.Equal(JanetSignal.Error, signal);
    }

    // --- Implicit conversions ---

    [Fact]
    public void ImplicitConversion_Double()
    {
        Janet j = 3.14;
        Assert.Equal(JanetType.Number, j.Type);
        Assert.Equal(3.14, j.AsNumber());
    }

    [Fact]
    public void ImplicitConversion_Int()
    {
        Janet j = 42;
        Assert.Equal(JanetType.Number, j.Type);
        Assert.Equal(42, j.AsInteger());
    }

    [Fact]
    public void ImplicitConversion_Bool()
    {
        Janet j = true;
        Assert.Equal(JanetType.Boolean, j.Type);
        Assert.True(j.AsBoolean());
    }

    [Fact]
    public void ImplicitConversion_String()
    {
        Janet j = "hello";
        Assert.Equal(JanetType.String, j.Type);
        Assert.Equal("hello", j.AsString());
    }

    // --- IsGcType ---

    [Fact]
    public void IsGcType_String_IsTrue()
    {
        var j = Janet.From("hello");
        Assert.True(j.IsGcType);
    }

    [Fact]
    public void IsGcType_Number_IsFalse()
    {
        Assert.False(Janet.From(42.0).IsGcType);
    }

    [Fact]
    public void IsGcType_Nil_IsFalse()
    {
        Assert.False(Janet.Nil.IsGcType);
    }

    [Fact]
    public void IsGcType_Boolean_IsFalse()
    {
        Assert.False(Janet.True.IsGcType);
    }
}
