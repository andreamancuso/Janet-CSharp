namespace JanetSharp;

/// <summary>
/// Janet value type tags, mirroring the JanetType enum in janet.h.
/// </summary>
public enum JanetType
{
    Number = 0,
    Nil = 1,
    Boolean = 2,
    Fiber = 3,
    String = 4,
    Symbol = 5,
    Keyword = 6,
    Array = 7,
    Tuple = 8,
    Table = 9,
    Struct = 10,
    Buffer = 11,
    Function = 12,
    CFunction = 13,
    Abstract = 14,
    Pointer = 15,
}
