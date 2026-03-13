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
