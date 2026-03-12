namespace JanetSharp;

/// <summary>
/// Janet value type tags, mirroring the JanetType enum in janet.h.
/// </summary>
public enum JanetType
{
    /// <summary>A 64-bit floating-point number (IEEE 754 double).</summary>
    Number = 0,
    /// <summary>The nil value, representing absence of a value.</summary>
    Nil = 1,
    /// <summary>A boolean value (true or false).</summary>
    Boolean = 2,
    /// <summary>A Janet fiber (coroutine).</summary>
    Fiber = 3,
    /// <summary>An immutable UTF-8 byte string.</summary>
    String = 4,
    /// <summary>An interned identifier used for variable names and bindings.</summary>
    Symbol = 5,
    /// <summary>An interned identifier prefixed with ':' in Janet syntax, used as lightweight enum values or map keys.</summary>
    Keyword = 6,
    /// <summary>A mutable, ordered collection of Janet values.</summary>
    Array = 7,
    /// <summary>An immutable, ordered collection of Janet values.</summary>
    Tuple = 8,
    /// <summary>A mutable hash map from Janet values to Janet values.</summary>
    Table = 9,
    /// <summary>An immutable hash map from Janet values to Janet values.</summary>
    Struct = 10,
    /// <summary>A mutable byte buffer.</summary>
    Buffer = 11,
    /// <summary>A Janet function defined in Janet code.</summary>
    Function = 12,
    /// <summary>A C function registered with the Janet runtime.</summary>
    CFunction = 13,
    /// <summary>An abstract type wrapping a native or user-defined object.</summary>
    Abstract = 14,
    /// <summary>A raw pointer value.</summary>
    Pointer = 15,
}
