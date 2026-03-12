namespace JanetSharp;

/// <summary>
/// Signal codes returned by janet_pcall, mirroring JanetSignal in janet.h.
/// </summary>
public enum JanetSignal
{
    /// <summary>Successful completion with no error.</summary>
    Ok = 0,
    /// <summary>An error occurred during evaluation or function invocation.</summary>
    Error = 1,
    /// <summary>A debug signal was raised (breakpoint).</summary>
    Debug = 2,
    /// <summary>A fiber yielded a value.</summary>
    Yield = 3,
}
