using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === JanetArray Tests ===

public class JanetArrayTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetArrayTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_Empty_HasZeroCount()
    {
        using var arr = JanetArray.Create();
        Assert.Equal(0, arr.Count);
    }

    [Fact]
    public void Push_And_Count()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.From(1.0));
        arr.Add(Janet.From(2.0));
        arr.Add(Janet.From(3.0));
        Assert.Equal(3, arr.Count);
    }

    [Fact]
    public void Indexer_Get()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.From(42.0));
        arr.Add(Janet.From(99.0));
        Assert.Equal(42.0, arr[0].AsNumber());
        Assert.Equal(99.0, arr[1].AsNumber());
    }

    [Fact]
    public void Indexer_Set()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.From(1.0));
        arr[0] = Janet.From(999.0);
        Assert.Equal(999.0, arr[0].AsNumber());
    }

    [Fact]
    public void Pop_ReturnsLastElement()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.From(10.0));
        arr.Add(Janet.From(20.0));
        var popped = arr.Pop();
        Assert.Equal(20.0, popped.AsNumber());
        Assert.Equal(1, arr.Count);
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.From(1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => arr[5]);
        Assert.Throws<ArgumentOutOfRangeException>(() => arr[-1]);
    }

    [Fact]
    public void Enumeration_Works()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.From(1.0));
        arr.Add(Janet.From(2.0));
        arr.Add(Janet.From(3.0));

        var values = new List<double>();
        foreach (var item in arr)
            values.Add(item.AsNumber());

        Assert.Equal([1.0, 2.0, 3.0], values);
    }

    [Fact]
    public void Contains_FindsValue()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.From(42.0));
        Assert.True(arr.Contains(Janet.From(42.0)));
        Assert.False(arr.Contains(Janet.From(99.0)));
    }

    [Fact]
    public void Clear_EmptiesArray()
    {
        using var arr = JanetArray.Create();
        arr.Add(Janet.From(1.0));
        arr.Add(Janet.From(2.0));
        arr.Clear();
        Assert.Equal(0, arr.Count);
    }
}

// === JanetTuple Tests ===

public class JanetTupleTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetTupleTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_WithValues()
    {
        using var t = JanetTuple.Create(Janet.From(1.0), Janet.From(2.0), Janet.From(3.0));
        Assert.Equal(3, t.Count);
    }

    [Fact]
    public void Indexer_ReturnsCorrectValues()
    {
        using var t = JanetTuple.Create(Janet.From(10.0), Janet.From(20.0));
        Assert.Equal(10.0, t[0].AsNumber());
        Assert.Equal(20.0, t[1].AsNumber());
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        using var t = JanetTuple.Create(Janet.From(1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => t[5]);
        Assert.Throws<ArgumentOutOfRangeException>(() => t[-1]);
    }

    [Fact]
    public void Empty_Tuple()
    {
        using var t = JanetTuple.Create();
        Assert.Equal(0, t.Count);
    }

    [Fact]
    public void Enumeration_Works()
    {
        using var t = JanetTuple.Create(Janet.From(1.0), Janet.From(2.0));
        var values = new List<double>();
        foreach (var item in t)
            values.Add(item.AsNumber());
        Assert.Equal([1.0, 2.0], values);
    }

    [Fact]
    public void Type_IsTuple()
    {
        using var t = JanetTuple.Create(Janet.From(1.0));
        Assert.Equal(JanetType.Tuple, t.Type);
    }
}

// === JanetBuffer Tests ===

public class JanetBufferTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetBufferTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_Empty_HasZeroCount()
    {
        using var buf = JanetBuffer.Create();
        Assert.Equal(0, buf.Count);
    }

    [Fact]
    public void WriteByte_And_Count()
    {
        using var buf = JanetBuffer.Create();
        buf.WriteByte(0x41);
        buf.WriteByte(0x42);
        Assert.Equal(2, buf.Count);
    }

    [Fact]
    public void WriteBytes_And_AsSpan()
    {
        using var buf = JanetBuffer.Create();
        byte[] data = [1, 2, 3, 4, 5];
        buf.WriteBytes(data);

        Assert.Equal(5, buf.Count);
        var span = buf.AsSpan();
        Assert.Equal(5, span.Length);
        for (int i = 0; i < 5; i++)
            Assert.Equal(data[i], span[i]);
    }

    [Fact]
    public void AsSpan_ReadBack()
    {
        using var buf = JanetBuffer.Create();
        buf.WriteByte((byte)'H');
        buf.WriteByte((byte)'i');

        var span = buf.AsSpan();
        Assert.Equal((byte)'H', span[0]);
        Assert.Equal((byte)'i', span[1]);
    }

    [Fact]
    public void SetCount_Truncates()
    {
        using var buf = JanetBuffer.Create();
        buf.WriteBytes(new byte[] { 1, 2, 3, 4, 5 });
        Assert.Equal(5, buf.Count);

        buf.SetCount(2);
        Assert.Equal(2, buf.Count);
    }

    [Fact]
    public void Type_IsBuffer()
    {
        using var buf = JanetBuffer.Create();
        Assert.Equal(JanetType.Buffer, buf.Type);
    }
}
