using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === JanetString Tests ===

public class JanetStringTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetStringTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_And_ToString_Roundtrips()
    {
        using var s = JanetString.Create("hello");
        Assert.Equal("hello", s.ToString());
    }

    [Fact]
    public void Length_ReturnsByteLength()
    {
        using var s = JanetString.Create("hello");
        Assert.Equal(5, s.Length);
    }

    [Fact]
    public void EmptyString_Works()
    {
        using var s = JanetString.Create("");
        Assert.Equal("", s.ToString());
        Assert.Equal(0, s.Length);
    }

    [Fact]
    public void AsSpan_ReturnsUtf8Bytes()
    {
        using var s = JanetString.Create("ABC");
        var span = s.AsSpan();
        Assert.Equal(3, span.Length);
        Assert.Equal((byte)'A', span[0]);
        Assert.Equal((byte)'B', span[1]);
        Assert.Equal((byte)'C', span[2]);
    }

    [Fact]
    public void Type_IsString()
    {
        using var s = JanetString.Create("test");
        Assert.Equal(JanetType.String, s.Type);
    }

    [Fact]
    public void Utf8_MultibyteCharacters()
    {
        using var s = JanetString.Create("\u00e9"); // é = 2 bytes in UTF-8
        Assert.Equal(2, s.Length);
        Assert.Equal("\u00e9", s.ToString());
    }

    [Fact]
    public void Janet_From_String_Creates_JanetString()
    {
        var j = Janet.From("hello");
        Assert.Equal(JanetType.String, j.Type);
        Assert.Equal("hello", j.AsString());
    }

    [Fact]
    public void Janet_ImplicitConversion_FromString()
    {
        Janet j = "hello";
        Assert.Equal(JanetType.String, j.Type);
        Assert.Equal("hello", j.AsString());
    }

    [Fact]
    public void Eval_String_Roundtrips()
    {
        var result = _runtime.Eval("\"hello world\"");
        Assert.Equal(JanetType.String, result.Type);
        Assert.Equal("hello world", result.AsString());
    }
}

// === JanetSymbol Tests ===

public class JanetSymbolTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetSymbolTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_And_ToString_Roundtrips()
    {
        using var s = JanetSymbol.Create("my-sym");
        Assert.Equal("my-sym", s.ToString());
    }

    [Fact]
    public void Type_IsSymbol()
    {
        using var s = JanetSymbol.Create("test");
        Assert.Equal(JanetType.Symbol, s.Type);
    }

    [Fact]
    public void Length_ReturnsByteLength()
    {
        using var s = JanetSymbol.Create("abc");
        Assert.Equal(3, s.Length);
    }

    [Fact]
    public void Eval_QuotedSymbol()
    {
        var result = _runtime.Eval("(quote my-symbol)");
        Assert.Equal(JanetType.Symbol, result.Type);
        Assert.Equal("my-symbol", result.AsString());
    }
}

// === JanetKeyword Tests ===

public class JanetKeywordTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetKeywordTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_And_ToString_Roundtrips()
    {
        using var k = JanetKeyword.Create("name");
        Assert.Equal("name", k.ToString());
    }

    [Fact]
    public void Type_IsKeyword()
    {
        using var k = JanetKeyword.Create("test");
        Assert.Equal(JanetType.Keyword, k.Type);
    }

    [Fact]
    public void Length_ReturnsByteLength()
    {
        using var k = JanetKeyword.Create("key");
        Assert.Equal(3, k.Length);
    }

    [Fact]
    public void Eval_Keyword()
    {
        var result = _runtime.Eval(":my-key");
        Assert.Equal(JanetType.Keyword, result.Type);
        Assert.Equal("my-key", result.AsString());
    }
}

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

// === JanetTable Tests ===

public class JanetTableTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetTableTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Create_Empty_HasZeroCount()
    {
        using var tbl = JanetTable.Create();
        Assert.Equal(0, tbl.Count);
    }

    [Fact]
    public void Put_And_Get()
    {
        using var tbl = JanetTable.Create();
        using var key = JanetKeyword.Create("name");
        using var val = JanetString.Create("Janet");

        tbl[key.Value] = val.Value;
        Assert.Equal(1, tbl.Count);

        var result = tbl[key.Value];
        Assert.Equal("Janet", result.AsString());
    }

    [Fact]
    public void ContainsKey_Works()
    {
        using var tbl = JanetTable.Create();
        using var key = JanetKeyword.Create("x");

        Assert.False(tbl.ContainsKey(key.Value));
        tbl[key.Value] = Janet.From(42.0);
        Assert.True(tbl.ContainsKey(key.Value));
    }

    [Fact]
    public void Remove_Works()
    {
        using var tbl = JanetTable.Create();
        using var key = JanetKeyword.Create("x");

        tbl[key.Value] = Janet.From(1.0);
        Assert.Equal(1, tbl.Count);

        bool removed = tbl.Remove(key.Value);
        Assert.True(removed);
        Assert.Equal(0, tbl.Count);
        Assert.False(tbl.ContainsKey(key.Value));
    }

    [Fact]
    public void Remove_NonExistent_ReturnsFalse()
    {
        using var tbl = JanetTable.Create();
        using var key = JanetKeyword.Create("missing");
        Assert.False(tbl.Remove(key.Value));
    }

    [Fact]
    public void TryGetValue_Works()
    {
        using var tbl = JanetTable.Create();
        using var key = JanetKeyword.Create("k");

        Assert.False(tbl.TryGetValue(key.Value, out _));

        tbl[key.Value] = Janet.From(99.0);
        Assert.True(tbl.TryGetValue(key.Value, out var val));
        Assert.Equal(99.0, val.AsNumber());
    }

    [Fact]
    public void Clear_EmptiesTable()
    {
        using var tbl = JanetTable.Create();
        tbl[Janet.From(1.0)] = Janet.From(10.0);
        tbl[Janet.From(2.0)] = Janet.From(20.0);
        Assert.Equal(2, tbl.Count);

        tbl.Clear();
        Assert.Equal(0, tbl.Count);
    }

    [Fact]
    public void NumberKeys_Work()
    {
        using var tbl = JanetTable.Create();
        tbl[Janet.From(1.0)] = Janet.From(100.0);
        tbl[Janet.From(2.0)] = Janet.From(200.0);
        Assert.Equal(100.0, tbl[Janet.From(1.0)].AsNumber());
        Assert.Equal(200.0, tbl[Janet.From(2.0)].AsNumber());
    }

    [Fact]
    public void GetEnumerator_EmptyTable_NoEntries()
    {
        using var tbl = JanetTable.Create();
        var entries = new List<KeyValuePair<Janet, Janet>>();
        foreach (var kv in tbl)
            entries.Add(kv);
        Assert.Empty(entries);
    }

    [Fact]
    public void GetEnumerator_ReturnsAllEntries()
    {
        using var tbl = JanetTable.Create();
        tbl[Janet.From("a")] = Janet.From(1.0);
        tbl[Janet.From("b")] = Janet.From(2.0);
        tbl[Janet.From("c")] = Janet.From(3.0);

        var dict = new Dictionary<string, double>();
        foreach (var kv in tbl)
            dict[kv.Key.AsString()] = kv.Value.AsNumber();

        Assert.Equal(3, dict.Count);
        Assert.Equal(1.0, dict["a"]);
        Assert.Equal(2.0, dict["b"]);
        Assert.Equal(3.0, dict["c"]);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        using var tbl = JanetTable.Create();
        tbl[Janet.From("x")] = Janet.From(10.0);
        tbl[Janet.From("y")] = Janet.From(20.0);

        var keys = tbl.Keys.Select(k => k.AsString()).OrderBy(k => k).ToList();
        Assert.Equal(["x", "y"], keys);
    }

    [Fact]
    public void Values_ReturnsAllValues()
    {
        using var tbl = JanetTable.Create();
        tbl[Janet.From("a")] = Janet.From(10.0);
        tbl[Janet.From("b")] = Janet.From(20.0);

        var values = tbl.Values.Select(v => v.AsNumber()).OrderBy(v => v).ToList();
        Assert.Equal([10.0, 20.0], values);
    }

    [Fact]
    public void CopyTo_Works()
    {
        using var tbl = JanetTable.Create();
        tbl[Janet.From("k")] = Janet.From(99.0);

        var array = new KeyValuePair<Janet, Janet>[3];
        tbl.CopyTo(array, 1);

        Assert.Equal("k", array[1].Key.AsString());
        Assert.Equal(99.0, array[1].Value.AsNumber());
    }

    [Fact]
    public void Linq_ToList_Works()
    {
        using var tbl = JanetTable.Create();
        tbl[Janet.From("p")] = Janet.From(1.0);
        tbl[Janet.From("q")] = Janet.From(2.0);

        var list = tbl.ToList();
        Assert.Equal(2, list.Count);
    }
}

// === JanetStruct Tests ===

public class JanetStructTests2 : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetStructTests2() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    [Fact]
    public void Eval_Struct_And_Read()
    {
        // Janet struct literal: (struct :a 1 :b 2)
        // Note: struct is a C-level function available with JANET_BOOTSTRAP
        var result = _runtime.Eval("(struct :a 1 :b 2)");
        Assert.Equal(JanetType.Struct, result.Type);

        using var st = result.AsStruct();
        Assert.Equal(2, st.Count);

        using var keyA = JanetKeyword.Create("a");
        using var keyB = JanetKeyword.Create("b");

        Assert.True(st.ContainsKey(keyA.Value));
        Assert.Equal(1.0, st[keyA.Value].AsNumber());
        Assert.Equal(2.0, st[keyB.Value].AsNumber());
    }

    [Fact]
    public void TryGetValue_Missing_ReturnsFalse()
    {
        var result = _runtime.Eval("(struct :x 10)");
        using var st = result.AsStruct();
        using var missing = JanetKeyword.Create("missing");

        Assert.False(st.TryGetValue(missing.Value, out _));
    }

    [Fact]
    public void Indexer_Missing_Throws()
    {
        var result = _runtime.Eval("(struct :x 10)");
        using var st = result.AsStruct();
        using var missing = JanetKeyword.Create("missing");

        Assert.Throws<KeyNotFoundException>(() => st[missing.Value]);
    }

    [Fact]
    public void GetEnumerator_ReturnsAllEntries()
    {
        var result = _runtime.Eval("(struct :a 1 :b 2 :c 3)");
        using var st = result.AsStruct();

        var dict = new Dictionary<string, double>();
        foreach (var kv in st)
            dict[kv.Key.AsString()] = kv.Value.AsNumber();

        Assert.Equal(3, dict.Count);
        Assert.Equal(1.0, dict["a"]);
        Assert.Equal(2.0, dict["b"]);
        Assert.Equal(3.0, dict["c"]);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        var result = _runtime.Eval("(struct :x 10 :y 20)");
        using var st = result.AsStruct();

        var keys = st.Keys.Select(k => k.AsString()).OrderBy(k => k).ToList();
        Assert.Equal(["x", "y"], keys);
    }

    [Fact]
    public void Values_ReturnsAllValues()
    {
        var result = _runtime.Eval("(struct :x 10 :y 20)");
        using var st = result.AsStruct();

        var values = st.Values.Select(v => v.AsNumber()).OrderBy(v => v).ToList();
        Assert.Equal([10.0, 20.0], values);
    }

    [Fact]
    public void Foreach_EmptyStruct_NoEntries()
    {
        var result = _runtime.Eval("(struct)");
        using var st = result.AsStruct();

        var entries = new List<KeyValuePair<Janet, Janet>>();
        foreach (var kv in st)
            entries.Add(kv);
        Assert.Empty(entries);
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
