using System.Collections;
using System.Runtime.InteropServices;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet array (mutable, ordered collection).
/// Implements IList&lt;Janet&gt; for .NET interop.
/// </summary>
public class JanetArray : JanetValue, IList<Janet>
{
    private JanetArray(Janet value) : base(value) { }

    /// <summary>
    /// Creates a new empty Janet array with the given initial capacity.
    /// </summary>
    public static JanetArray Create(int capacity = 0)
    {
        var raw = new Janet(NativeMethods.shim_array_new(capacity));
        return new JanetArray(raw);
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be an array.
    /// </summary>
    internal static JanetArray Wrap(Janet value)
    {
        if (value.Type != JanetType.Array)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetArray.");
        return new JanetArray(value);
    }

    public int Count => NativeMethods.shim_array_count(Value.RawValue);

    public bool IsReadOnly => false;

    public Janet this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return new Janet(NativeMethods.shim_array_get(Value.RawValue, index));
        }
        set
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            NativeMethods.shim_array_set(Value.RawValue, index, value.RawValue);
        }
    }

    public void Add(Janet item) => NativeMethods.shim_array_push(Value.RawValue, item.RawValue);

    /// <summary>
    /// Removes and returns the last element of the array.
    /// </summary>
    public Janet Pop() => new Janet(NativeMethods.shim_array_pop(Value.RawValue));

    public void Clear()
    {
        // Pop all elements
        while (Count > 0)
            NativeMethods.shim_array_pop(Value.RawValue);
    }

    public bool Contains(Janet item)
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            if (new Janet(NativeMethods.shim_array_get(Value.RawValue, i)) == item)
                return true;
        }
        return false;
    }

    public void CopyTo(Janet[] array, int arrayIndex)
    {
        int count = Count;
        for (int i = 0; i < count; i++)
            array[arrayIndex + i] = new Janet(NativeMethods.shim_array_get(Value.RawValue, i));
    }

    public int IndexOf(Janet item)
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            if (new Janet(NativeMethods.shim_array_get(Value.RawValue, i)) == item)
                return i;
        }
        return -1;
    }

    public void Insert(int index, Janet item) =>
        throw new NotSupportedException("Janet arrays do not support insertion at arbitrary indices.");

    public void RemoveAt(int index) =>
        throw new NotSupportedException("Janet arrays do not support removal at arbitrary indices.");

    public bool Remove(Janet item) =>
        throw new NotSupportedException("Janet arrays do not support removal by value.");

    public IEnumerator<Janet> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
            yield return new Janet(NativeMethods.shim_array_get(Value.RawValue, i));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
