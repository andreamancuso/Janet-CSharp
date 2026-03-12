namespace JanetSharp;

/// <summary>
/// Signal codes returned by janet_pcall, mirroring JanetSignal in janet.h.
/// </summary>
public enum JanetSignal
{
    Ok = 0,
    Error = 1,
    Debug = 2,
    Yield = 3,
}
