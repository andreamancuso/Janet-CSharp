using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

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
        Assert.Empty(tbl);
    }

    [Fact]
    public void Put_And_Get()
    {
        using var tbl = JanetTable.Create();
        using var key = JanetKeyword.Create("name");
        using var val = JanetString.Create("Janet");

        tbl[key.Value] = val.Value;
        Assert.Single(tbl);

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
        Assert.Single(tbl);

        bool removed = tbl.Remove(key.Value);
        Assert.True(removed);
        Assert.Empty(tbl);
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
        Assert.Empty(tbl);
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

// === JanetStruct (immutable map) Tests ===

public class JanetStructCollectionTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public JanetStructCollectionTests() => _runtime = new JanetRuntime();
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
