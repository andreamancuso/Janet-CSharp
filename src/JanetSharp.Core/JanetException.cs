namespace JanetSharp;

/// <summary>
/// Exception thrown when Janet evaluation or function calls return an error signal.
/// </summary>
public class JanetException : Exception
{
    /// <summary>
    /// The Janet error value (typically a string describing the error).
    /// </summary>
    public Janet ErrorValue { get; }

    /// <summary>
    /// The signal that caused the exception.
    /// </summary>
    public JanetSignal Signal { get; }

    public JanetException(Janet errorValue, JanetSignal signal)
        : base($"Janet error (signal={signal})")
    {
        ErrorValue = errorValue;
        Signal = signal;
    }

    public JanetException(string message) : base(message) { }

    public JanetException(string message, Exception innerException)
        : base(message, innerException) { }
}
