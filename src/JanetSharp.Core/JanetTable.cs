using System.Collections;
using System.Collections.ObjectModel;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet table (mutable hash map).
/// Implements IDictionary&lt;Janet, Janet&gt; for .NET interop.
/// </summary>
public class JanetTable : JanetValue, IDictionary<Janet, Janet>
{
    private JanetTable(Janet value) : base(value) { }

    /// <summary>
    /// Creates a new empty Janet table with the given initial capacity.
    /// </summary>
    public static JanetTable Create(int capacity = 0)
    {
        var raw = new Janet(NativeMethods.shim_table_new(capacity));
        return new JanetTable(raw);
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a table.
    /// </summary>
    internal static JanetTable Wrap(Janet value)
    {
        if (value.Type != JanetType.Table)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetTable.");
        return new JanetTable(value);
    }

    /// <inheritdoc />
    public int Count => NativeMethods.shim_table_count(Value.RawValue);

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public Janet this[Janet key]
    {
        get => new Janet(NativeMethods.shim_table_get(Value.RawValue, key.RawValue));
        set => NativeMethods.shim_table_put(Value.RawValue, key.RawValue, value.RawValue);
    }

    /// <inheritdoc />
    public void Add(Janet key, Janet value)
    {
        if (ContainsKey(key))
            throw new ArgumentException("An element with the same key already exists.", nameof(key));
        NativeMethods.shim_table_put(Value.RawValue, key.RawValue, value.RawValue);
    }

    /// <inheritdoc />
    public bool ContainsKey(Janet key)
    {
        var result = new Janet(NativeMethods.shim_table_get(Value.RawValue, key.RawValue));
        return !result.IsNil;
    }

    /// <inheritdoc />
    public bool Remove(Janet key)
    {
        if (!ContainsKey(key))
            return false;
        NativeMethods.shim_table_remove(Value.RawValue, key.RawValue);
        return true;
    }

    /// <inheritdoc />
    public bool TryGetValue(Janet key, out Janet value)
    {
        var result = new Janet(NativeMethods.shim_table_get(Value.RawValue, key.RawValue));
        if (result.IsNil)
        {
            value = Janet.Nil;
            return false;
        }
        value = result;
        return true;
    }

    /// <inheritdoc />
    public void Clear() => NativeMethods.shim_table_clear(Value.RawValue);

    // === IDictionary<> enumeration members ===

    /// <inheritdoc />
    public ICollection<Janet> Keys
    {
        get
        {
            CollectEntries(out var keys, out _);
            return new ReadOnlyCollection<Janet>(keys);
        }
    }

    /// <inheritdoc />
    public ICollection<Janet> Values
    {
        get
        {
            CollectEntries(out _, out var values);
            return new ReadOnlyCollection<Janet>(values);
        }
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<Janet, Janet> item) => Add(item.Key, item.Value);

    /// <inheritdoc />
    public bool Contains(KeyValuePair<Janet, Janet> item)
    {
        if (!TryGetValue(item.Key, out var val))
            return false;
        return val == item.Value;
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<Janet, Janet>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        int count = Count;
        if (arrayIndex < 0 || arrayIndex + count > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        CollectEntries(out var keys, out var values);
        for (int i = 0; i < keys.Length; i++)
            array[arrayIndex + i] = new KeyValuePair<Janet, Janet>(keys[i], values[i]);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<Janet, Janet> item)
    {
        if (!Contains(item))
            return false;
        return Remove(item.Key);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Janet, Janet>> GetEnumerator()
    {
        CollectEntries(out var keys, out var values);
        for (int i = 0; i < keys.Length; i++)
            yield return new KeyValuePair<Janet, Janet>(keys[i], values[i]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void CollectEntries(out Janet[] keys, out Janet[] values)
    {
        int count = Count;
        if (count == 0)
        {
            keys = [];
            values = [];
            return;
        }

        var rawKeys = new long[count];
        var rawValues = new long[count];
        int written = NativeMethods.shim_dictionary_collect(Value.RawValue, rawKeys, rawValues, count);

        keys = new Janet[written];
        values = new Janet[written];
        for (int i = 0; i < written; i++)
        {
            keys[i] = new Janet(rawKeys[i]);
            values[i] = new Janet(rawValues[i]);
        }
    }
}
