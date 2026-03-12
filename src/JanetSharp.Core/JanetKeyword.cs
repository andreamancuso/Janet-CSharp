using System.Runtime.InteropServices;
using System.Text;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet keyword value.
/// Keywords are interned identifiers prefixed with ':' in Janet syntax.
/// </summary>
public class JanetKeyword : JanetValue
{
    private JanetKeyword(Janet value) : base(value) { }

    /// <summary>
    /// Creates a new Janet keyword from a .NET string (without the ':' prefix).
    /// </summary>
    public static JanetKeyword Create(string s)
    {
        var raw = new Janet(NativeMethods.shim_wrap_keyword(s));
        return new JanetKeyword(raw);
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a keyword.
    /// </summary>
    internal static JanetKeyword Wrap(Janet value)
    {
        if (value.Type != JanetType.Keyword)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetKeyword.");
        return new JanetKeyword(value);
    }

    /// <summary>
    /// The byte length of the keyword name (without ':' prefix).
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
