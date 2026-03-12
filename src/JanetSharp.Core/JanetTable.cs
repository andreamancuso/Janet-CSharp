using System.Collections;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet table (mutable hash map).
/// Implements IDictionary&lt;Janet, Janet&gt; for .NET interop.
/// Note: Enumeration (Keys, Values, GetEnumerator) is deferred — requires table iteration support in the shim.
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

    // === IDictionary<> enumeration members (deferred — require shim iteration support) ===

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Table key enumeration requires iteration support in the native shim.</exception>
    public ICollection<Janet> Keys =>
        throw new NotSupportedException("Table key enumeration requires iteration support in the native shim (planned for a future phase).");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Table value enumeration requires iteration support in the native shim.</exception>
    public ICollection<Janet> Values =>
        throw new NotSupportedException("Table value enumeration requires iteration support in the native shim (planned for a future phase).");

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
    /// <exception cref="NotSupportedException">Table enumeration requires iteration support in the native shim.</exception>
    public void CopyTo(KeyValuePair<Janet, Janet>[] array, int arrayIndex) =>
        throw new NotSupportedException("Table enumeration requires iteration support in the native shim.");

    /// <inheritdoc />
    public bool Remove(KeyValuePair<Janet, Janet> item)
    {
        if (!Contains(item))
            return false;
        return Remove(item.Key);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Table enumeration requires iteration support in the native shim.</exception>
    public IEnumerator<KeyValuePair<Janet, Janet>> GetEnumerator() =>
        throw new NotSupportedException("Table enumeration requires iteration support in the native shim (planned for a future phase).");

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
