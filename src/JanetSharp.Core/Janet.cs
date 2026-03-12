using System.Runtime.InteropServices;
using System.Text;

namespace JanetSharp;

/// <summary>
/// A 64-bit NaN-boxed Janet value. This struct mirrors the native Janet union
/// and can be passed directly across the P/Invoke boundary.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 8)]
public readonly struct Janet : IEquatable<Janet>
{
    [FieldOffset(0)]
    internal readonly long RawValue;

    internal Janet(long raw) => RawValue = raw;

    // === Factory Methods ===

    public static Janet Nil => new(NativeMethods.shim_wrap_nil());

    public static Janet True => new(NativeMethods.shim_wrap_boolean(1));

    public static Janet False => new(NativeMethods.shim_wrap_boolean(0));

    public static Janet From(double value) => new(NativeMethods.shim_wrap_number(value));

    public static Janet From(bool value) => new(NativeMethods.shim_wrap_boolean(value ? 1 : 0));

    public static Janet From(int value) => new(NativeMethods.shim_wrap_integer(value));

    public static Janet From(string value) => new(NativeMethods.shim_wrap_string(value));

    // === Implicit Conversions ===

    public static implicit operator Janet(double value) => From(value);
    public static implicit operator Janet(int value) => From(value);
    public static implicit operator Janet(bool value) => From(value);
    public static implicit operator Janet(string value) => From(value);

    // === Type Inspection ===

    public JanetType Type => (JanetType)NativeMethods.shim_type(RawValue);

    public bool IsNil => Type == JanetType.Nil;

    public bool IsTruthy => NativeMethods.shim_truthy(RawValue) != 0;

    /// <summary>
    /// Returns true if this value is a reference type that participates in Janet's GC.
    /// Primitives (Number, Nil, Boolean) are encoded entirely within the NaN-box
    /// and do not need GC rooting.
    /// </summary>
    internal bool IsGcType => Type is not (JanetType.Number or JanetType.Nil or JanetType.Boolean);

    // === Unwrap ===

    public double AsNumber()
    {
        if (Type != JanetType.Number)
            throw new InvalidOperationException($"Cannot unwrap {Type} as Number.");
        return NativeMethods.shim_unwrap_number(RawValue);
    }

    public bool AsBoolean()
    {
        if (Type != JanetType.Boolean)
            throw new InvalidOperationException($"Cannot unwrap {Type} as Boolean.");
        return NativeMethods.shim_unwrap_boolean(RawValue) != 0;
    }

    public int AsInteger()
    {
        if (Type != JanetType.Number)
            throw new InvalidOperationException($"Cannot unwrap {Type} as Integer.");
        return NativeMethods.shim_unwrap_integer(RawValue);
    }

    /// <summary>
    /// Unwraps this value as a .NET string. Only valid for String, Symbol, or Keyword types.
    /// </summary>
    public unsafe string AsString()
    {
        if (Type is not (JanetType.String or JanetType.Symbol or JanetType.Keyword))
            throw new InvalidOperationException($"Cannot unwrap {Type} as string.");
        int len = NativeMethods.shim_string_length(RawValue);
        if (len == 0) return string.Empty;
        var ptr = NativeMethods.shim_unwrap_string_ptr(RawValue);
        return Encoding.UTF8.GetString(new ReadOnlySpan<byte>((void*)ptr, len));
    }

    /// <summary>
    /// Wraps this value as a JanetArray. Only valid for Array type.
    /// </summary>
    public JanetArray AsArray() => JanetArray.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetTuple. Only valid for Tuple type.
    /// </summary>
    public JanetTuple AsTuple() => JanetTuple.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetTable. Only valid for Table type.
    /// </summary>
    public JanetTable AsTable() => JanetTable.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetStruct. Only valid for Struct type.
    /// </summary>
    public JanetStruct AsStruct() => JanetStruct.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetBuffer. Only valid for Buffer type.
    /// </summary>
    public JanetBuffer AsBuffer() => JanetBuffer.Wrap(this);

    // === Equality ===

    public bool Equals(Janet other) => RawValue == other.RawValue;

    public override bool Equals(object? obj) => obj is Janet other && Equals(other);

    public override int GetHashCode() => RawValue.GetHashCode();

    public static bool operator ==(Janet left, Janet right) => left.Equals(right);

    public static bool operator !=(Janet left, Janet right) => !left.Equals(right);

    public override string ToString() => $"Janet({Type})";
}
