using System.Collections;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet struct (immutable hash map).
/// Implements IReadOnlyDictionary&lt;Janet, Janet&gt; for .NET interop.
/// Note: Enumeration (Keys, Values, GetEnumerator) is deferred — requires struct iteration support in the shim.
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

    // === Enumeration (deferred — requires shim iteration support) ===

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Struct key enumeration requires iteration support in the native shim.</exception>
    public IEnumerable<Janet> Keys =>
        throw new NotSupportedException("Struct key enumeration requires iteration support in the native shim (planned for a future phase).");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Struct value enumeration requires iteration support in the native shim.</exception>
    public IEnumerable<Janet> Values =>
        throw new NotSupportedException("Struct value enumeration requires iteration support in the native shim (planned for a future phase).");

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Struct enumeration requires iteration support in the native shim.</exception>
    public IEnumerator<KeyValuePair<Janet, Janet>> GetEnumerator() =>
        throw new NotSupportedException("Struct enumeration requires iteration support in the native shim (planned for a future phase).");

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
