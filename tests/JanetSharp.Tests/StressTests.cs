using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Stress Tests ===

public class StressTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public StressTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void GC_Pressure_1000_Values()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var s = JanetString.Create($"string_{i}");
            _ = s.ToString();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // If we get here without crashing, the GC root/unroot lifecycle is correct
        var result = _runtime.Eval("(+ 1 1)");
        Assert.Equal(2.0, result.AsNumber());
    }

    [Fact]
    public void Callback_HotLoop_10000_Invocations()
    {
        int callCount = 0;
        using var cb = _runtime.Register("hot-fn", args =>
        {
            callCount++;
            return Janet.From(args[0].AsNumber() + 1);
        });

        // Build Janet code that calls hot-fn 10000 times in a loop
        // Since we don't have loop/defn under JANET_BOOTSTRAP, we use a recursive fn
        var fnVal = _runtime.Eval("(fn [x] (hot-fn x))");
        using var fn = fnVal.AsFunction();

        for (int i = 0; i < 10_000; i++)
        {
            var result = fn.Invoke(Janet.From((double)i));
            Assert.Equal(i + 1.0, result.AsNumber());
        }

        Assert.Equal(10_000, callCount);
    }

    [Fact]
    public void Array_Churn_1000_Elements()
    {
        using var arr = JanetArray.Create(1000);

        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < 1000; i++)
                arr.Add(Janet.From((double)i));

            Assert.Equal(1000, arr.Count);

            for (int i = 0; i < 1000; i++)
                arr.Pop();

            Assert.Equal(0, arr.Count);
        }
    }

    [Fact]
    public void Table_Churn_1000_Entries()
    {
        using var tbl = JanetTable.Create(1000);

        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < 1000; i++)
                tbl[Janet.From((double)i)] = Janet.From((double)(i * 10));

            Assert.Equal(1000, tbl.Count);

            for (int i = 0; i < 1000; i++)
                tbl.Remove(Janet.From((double)i));

            Assert.Equal(0, tbl.Count);
        }
    }

    [Fact]
    public void String_Creation_1000()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var s = JanetString.Create($"test_string_{i}_{new string('x', 100)}");
            Assert.True(s.Length > 0);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Runtime still functional
        Assert.Equal(2.0, _runtime.Eval("(+ 1 1)").AsNumber());
    }

    [Fact]
    public void Buffer_Churn_LargeWrites()
    {
        using var buf = JanetBuffer.Create(0);
        var data = new byte[1024];
        Array.Fill(data, (byte)0xAB);

        for (int i = 0; i < 100; i++)
        {
            buf.WriteBytes(data);
        }

        Assert.Equal(100 * 1024, buf.Count);

        // Truncate and refill
        buf.SetCount(0);
        Assert.Equal(0, buf.Count);

        for (int i = 0; i < 100; i++)
            buf.WriteBytes(data);

        Assert.Equal(100 * 1024, buf.Count);
    }

    [Fact]
    public void Eval_Repeated_1000_Times()
    {
        for (int i = 0; i < 1000; i++)
        {
            var result = _runtime.Eval("(+ 1 2 3)");
            Assert.Equal(6.0, result.AsNumber());
        }
    }
}
