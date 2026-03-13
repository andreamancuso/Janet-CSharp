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

    /// <summary>Gets the Janet nil value.</summary>
    public static Janet Nil => new(NativeMethods.shim_wrap_nil());

    /// <summary>Gets the Janet boolean true value.</summary>
    public static Janet True => new(NativeMethods.shim_wrap_boolean(1));

    /// <summary>Gets the Janet boolean false value.</summary>
    public static Janet False => new(NativeMethods.shim_wrap_boolean(0));

    /// <summary>Creates a Janet number from a double.</summary>
    /// <param name="value">The numeric value.</param>
    /// <returns>A Janet value wrapping the number.</returns>
    public static Janet From(double value) => new(NativeMethods.shim_wrap_number(value));

    /// <summary>Creates a Janet boolean from a bool.</summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>A Janet value wrapping the boolean.</returns>
    public static Janet From(bool value) => new(NativeMethods.shim_wrap_boolean(value ? 1 : 0));

    /// <summary>Creates a Janet number from an integer.</summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A Janet value wrapping the integer as a number.</returns>
    public static Janet From(int value) => new(NativeMethods.shim_wrap_integer(value));

    /// <summary>Creates a Janet string from a .NET string (converted to UTF-8).</summary>
    /// <param name="value">The string value.</param>
    /// <returns>A Janet value wrapping the string.</returns>
    public static Janet From(string value) => new(NativeMethods.shim_wrap_string(value));

    // === Implicit Conversions ===

    /// <summary>Implicitly converts a double to a Janet number.</summary>
    /// <param name="value">The numeric value.</param>
    public static implicit operator Janet(double value) => From(value);
    /// <summary>Implicitly converts an int to a Janet number.</summary>
    /// <param name="value">The integer value.</param>
    public static implicit operator Janet(int value) => From(value);
    /// <summary>Implicitly converts a bool to a Janet boolean.</summary>
    /// <param name="value">The boolean value.</param>
    public static implicit operator Janet(bool value) => From(value);
    /// <summary>Implicitly converts a string to a Janet string.</summary>
    /// <param name="value">The string value.</param>
    public static implicit operator Janet(string value) => From(value);

    // === Type Inspection ===

    /// <summary>Gets the Janet type tag for this value.</summary>
    public JanetType Type => (JanetType)NativeMethods.shim_type(RawValue);

    /// <summary>Returns true if this value is nil.</summary>
    public bool IsNil => Type == JanetType.Nil;

    /// <summary>Returns true if this value is truthy (not nil and not false).</summary>
    public bool IsTruthy => NativeMethods.shim_truthy(RawValue) != 0;

    /// <summary>
    /// Returns true if this value is a reference type that participates in Janet's GC.
    /// Primitives (Number, Nil, Boolean) are encoded entirely within the NaN-box
    /// and do not need GC rooting.
    /// </summary>
    internal bool IsGcType => Type is not (JanetType.Number or JanetType.Nil or JanetType.Boolean);

    // === Unwrap ===

    /// <summary>Unwraps this value as a double. Only valid for Number type.</summary>
    /// <returns>The numeric value.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Number.</exception>
    public double AsNumber()
    {
        if (Type != JanetType.Number)
            throw new InvalidOperationException($"Cannot unwrap {Type} as Number.");
        return NativeMethods.shim_unwrap_number(RawValue);
    }

    /// <summary>Unwraps this value as a bool. Only valid for Boolean type.</summary>
    /// <returns>The boolean value.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Boolean.</exception>
    public bool AsBoolean()
    {
        if (Type != JanetType.Boolean)
            throw new InvalidOperationException($"Cannot unwrap {Type} as Boolean.");
        return NativeMethods.shim_unwrap_boolean(RawValue) != 0;
    }

    /// <summary>Unwraps this value as an int. Only valid for Number type.</summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Number.</exception>
    public int AsInteger()
    {
        if (Type != JanetType.Number)
            throw new InvalidOperationException($"Cannot unwrap {Type} as Integer.");
        return NativeMethods.shim_unwrap_integer(RawValue);
    }

    /// <summary>
    /// Unwraps this value as a .NET string. Only valid for String, Symbol, or Keyword types.
    /// </summary>
    /// <returns>The string value decoded from UTF-8.</returns>
    /// <exception cref="InvalidOperationException">This value is not a String, Symbol, or Keyword.</exception>
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
    /// <returns>A GC-rooted JanetArray wrapper.</returns>
    /// <exception cref="InvalidOperationException">This value is not an Array.</exception>
    public JanetArray AsArray() => JanetArray.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetTuple. Only valid for Tuple type.
    /// </summary>
    /// <returns>A GC-rooted JanetTuple wrapper.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Tuple.</exception>
    public JanetTuple AsTuple() => JanetTuple.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetTable. Only valid for Table type.
    /// </summary>
    /// <returns>A GC-rooted JanetTable wrapper.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Table.</exception>
    public JanetTable AsTable() => JanetTable.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetStruct. Only valid for Struct type.
    /// </summary>
    /// <returns>A GC-rooted JanetStruct wrapper.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Struct.</exception>
    public JanetStruct AsStruct() => JanetStruct.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetBuffer. Only valid for Buffer type.
    /// </summary>
    /// <returns>A GC-rooted JanetBuffer wrapper.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Buffer.</exception>
    public JanetBuffer AsBuffer() => JanetBuffer.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetFunction. Only valid for Function type.
    /// </summary>
    /// <returns>A GC-rooted JanetFunction wrapper.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Function.</exception>
    public JanetFunction AsFunction() => JanetFunction.Wrap(this);

    /// <summary>
    /// Wraps this value as a JanetFiber. Only valid for Fiber type.
    /// </summary>
    /// <returns>A GC-rooted JanetFiber wrapper.</returns>
    /// <exception cref="InvalidOperationException">This value is not a Fiber.</exception>
    public JanetFiber AsFiber() => JanetFiber.Wrap(this);

    // === Equality ===

    /// <inheritdoc />
    public bool Equals(Janet other) => RawValue == other.RawValue;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Janet other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => RawValue.GetHashCode();

    /// <summary>Returns true if two Janet values have the same raw representation.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if equal.</returns>
    public static bool operator ==(Janet left, Janet right) => left.Equals(right);

    /// <summary>Returns true if two Janet values have different raw representations.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if not equal.</returns>
    public static bool operator !=(Janet left, Janet right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"Janet({Type})";
}
