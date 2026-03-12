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

    /// <summary>
    /// Creates a JanetException from a Janet error value and signal code.
    /// </summary>
    /// <param name="errorValue">The Janet value describing the error.</param>
    /// <param name="signal">The signal code that triggered the exception.</param>
    public JanetException(Janet errorValue, JanetSignal signal)
        : base($"Janet error (signal={signal})")
    {
        ErrorValue = errorValue;
        Signal = signal;
    }

    /// <summary>
    /// Creates a JanetException with a message string.
    /// </summary>
    /// <param name="message">The error message.</param>
    public JanetException(string message) : base(message) { }

    /// <summary>
    /// Creates a JanetException with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public JanetException(string message, Exception innerException)
        : base(message, innerException) { }
}
