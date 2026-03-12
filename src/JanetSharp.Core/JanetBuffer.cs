using System.Runtime.InteropServices;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet buffer (mutable byte sequence).
/// </summary>
public class JanetBuffer : JanetValue
{
    private JanetBuffer(Janet value) : base(value) { }

    /// <summary>
    /// Creates a new empty Janet buffer with the given initial capacity.
    /// </summary>
    public static JanetBuffer Create(int capacity = 0)
    {
        var raw = new Janet(NativeMethods.shim_buffer_new(capacity));
        return new JanetBuffer(raw);
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a buffer.
    /// </summary>
    internal static JanetBuffer Wrap(Janet value)
    {
        if (value.Type != JanetType.Buffer)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetBuffer.");
        return new JanetBuffer(value);
    }

    /// <summary>
    /// The number of bytes currently in the buffer.
    /// </summary>
    public int Count => NativeMethods.shim_buffer_count(Value.RawValue);

    /// <summary>
    /// Appends a single byte to the buffer.
    /// </summary>
    public void WriteByte(byte b) => NativeMethods.shim_buffer_push_u8(Value.RawValue, b);

    /// <summary>
    /// Appends a span of bytes to the buffer.
    /// </summary>
    public unsafe void WriteBytes(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;
        fixed (byte* ptr = data)
        {
            NativeMethods.shim_buffer_push_bytes(Value.RawValue, (IntPtr)ptr, data.Length);
        }
    }

    /// <summary>
    /// Returns a read-only span over the buffer's current contents.
    /// </summary>
    public unsafe ReadOnlySpan<byte> AsSpan()
    {
        var ptr = NativeMethods.shim_buffer_data_ptr(Value.RawValue);
        return new ReadOnlySpan<byte>((void*)ptr, Count);
    }

    /// <summary>
    /// Sets the byte count of the buffer (must be &lt;= current capacity).
    /// Can be used to truncate the buffer.
    /// </summary>
    public void SetCount(int count) => NativeMethods.shim_buffer_setcount(Value.RawValue, count);

    /// <summary>
    /// Ensures the buffer has at least the given capacity.
    /// </summary>
    public void EnsureCapacity(int capacity) => NativeMethods.shim_buffer_ensure(Value.RawValue, capacity);
}
