namespace JanetSharp;

/// <summary>
/// Status codes for Janet fibers, mirroring JanetFiberStatus in janet.h.
/// </summary>
public enum JanetFiberStatus
{
    /// <summary>The fiber has finished executing and cannot be resumed.</summary>
    Dead = 0,
    /// <summary>The fiber terminated with an error.</summary>
    Error = 1,
    /// <summary>The fiber hit a debug breakpoint.</summary>
    Debug = 2,
    /// <summary>The fiber yielded a value and is waiting to be resumed.</summary>
    Pending = 3,
    /// <summary>The fiber was created but has not yet been resumed.</summary>
    New = 14,
    /// <summary>The fiber is currently executing (you are inside it).</summary>
    Alive = 15,
}
