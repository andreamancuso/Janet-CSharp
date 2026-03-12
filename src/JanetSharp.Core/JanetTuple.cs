using System.Collections;
using System.Runtime.InteropServices;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet tuple (immutable, ordered collection).
/// Implements IReadOnlyList&lt;Janet&gt; for .NET interop.
/// </summary>
public class JanetTuple : JanetValue, IReadOnlyList<Janet>
{
    private JanetTuple(Janet value) : base(value) { }

    /// <summary>
    /// Creates a new Janet tuple from the given values.
    /// </summary>
    public static unsafe JanetTuple Create(params Janet[] values)
    {
        long rawTuple;
        if (values.Length == 0)
        {
            rawTuple = NativeMethods.shim_tuple_n(IntPtr.Zero, 0);
        }
        else
        {
            // Janet structs are 8 bytes (same as long), so we can pin and pass directly
            fixed (Janet* ptr = values)
            {
                rawTuple = NativeMethods.shim_tuple_n((IntPtr)ptr, values.Length);
            }
        }
        return new JanetTuple(new Janet(rawTuple));
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a tuple.
    /// </summary>
    internal static JanetTuple Wrap(Janet value)
    {
        if (value.Type != JanetType.Tuple)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetTuple.");
        return new JanetTuple(value);
    }

    public int Count => NativeMethods.shim_tuple_length(Value.RawValue);

    public Janet this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return new Janet(NativeMethods.shim_tuple_get(Value.RawValue, index));
        }
    }

    public IEnumerator<Janet> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
            yield return new Janet(NativeMethods.shim_tuple_get(Value.RawValue, i));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
