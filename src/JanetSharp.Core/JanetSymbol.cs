using System.Runtime.InteropServices;
using System.Text;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet symbol value.
/// Symbols are interned identifiers in Janet.
/// </summary>
public class JanetSymbol : JanetValue
{
    private JanetSymbol(Janet value) : base(value) { }

    /// <summary>
    /// Creates a new Janet symbol from a .NET string.
    /// </summary>
    public static JanetSymbol Create(string s)
    {
        var raw = new Janet(NativeMethods.shim_wrap_symbol(s));
        return new JanetSymbol(raw);
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a symbol.
    /// </summary>
    internal static JanetSymbol Wrap(Janet value)
    {
        if (value.Type != JanetType.Symbol)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetSymbol.");
        return new JanetSymbol(value);
    }

    /// <summary>
    /// The byte length of the symbol name.
    /// </summary>
    public int Length => NativeMethods.shim_string_length(Value.RawValue);

    /// <summary>
    /// Returns a read-only span over the raw UTF-8 bytes.
    /// </summary>
    public unsafe ReadOnlySpan<byte> AsSpan()
    {
        var ptr = NativeMethods.shim_unwrap_string_ptr(Value.RawValue);
        return new ReadOnlySpan<byte>((void*)ptr, Length);
    }

    public override string ToString()
    {
        var span = AsSpan();
        if (span.IsEmpty) return string.Empty;
        return Encoding.UTF8.GetString(span);
    }
}
