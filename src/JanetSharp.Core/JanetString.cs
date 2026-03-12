using System.Runtime.InteropServices;
using System.Text;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet string value.
/// Janet strings are immutable byte sequences (typically UTF-8).
/// </summary>
public class JanetString : JanetValue
{
    private JanetString(Janet value) : base(value) { }

    /// <summary>
    /// Creates a new Janet string from a .NET string (converted to UTF-8).
    /// </summary>
    public static JanetString Create(string s)
    {
        var raw = new Janet(NativeMethods.shim_wrap_string(s));
        return new JanetString(raw);
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a string.
    /// </summary>
    internal static JanetString Wrap(Janet value)
    {
        if (value.Type != JanetType.String)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetString.");
        return new JanetString(value);
    }

    /// <summary>
    /// The byte length of the string.
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
