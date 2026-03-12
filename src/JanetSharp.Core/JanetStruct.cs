using System.Collections;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet struct (immutable hash map).
/// Implements IReadOnlyDictionary&lt;Janet, Janet&gt; for .NET interop.
/// </summary>
public class JanetStruct : JanetValue, IReadOnlyDictionary<Janet, Janet>
{
    private JanetStruct(Janet value) : base(value) { }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a struct.
    /// Structs are created by Janet code (e.g., via eval); there is no direct C# creation API.
    /// </summary>
    internal static JanetStruct Wrap(Janet value)
    {
        if (value.Type != JanetType.Struct)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetStruct.");
        return new JanetStruct(value);
    }

    /// <inheritdoc />
    public int Count => NativeMethods.shim_struct_length(Value.RawValue);

    /// <inheritdoc />
    public Janet this[Janet key]
    {
        get
        {
            var result = new Janet(NativeMethods.shim_struct_get(Value.RawValue, key.RawValue));
            if (result.IsNil)
                throw new KeyNotFoundException("The given key was not present in the Janet struct.");
            return result;
        }
    }

    /// <inheritdoc />
    public bool ContainsKey(Janet key)
    {
        var result = new Janet(NativeMethods.shim_struct_get(Value.RawValue, key.RawValue));
        return !result.IsNil;
    }

    /// <inheritdoc />
    public bool TryGetValue(Janet key, out Janet value)
    {
        var result = new Janet(NativeMethods.shim_struct_get(Value.RawValue, key.RawValue));
        if (result.IsNil)
        {
            value = Janet.Nil;
            return false;
        }
        value = result;
        return true;
    }

    // === Enumeration ===

    /// <inheritdoc />
    public IEnumerable<Janet> Keys
    {
        get
        {
            CollectEntries(out var keys, out _);
            return keys;
        }
    }

    /// <inheritdoc />
    public IEnumerable<Janet> Values
    {
        get
        {
            CollectEntries(out _, out var values);
            return values;
        }
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
