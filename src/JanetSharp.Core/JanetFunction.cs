namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet function value.
/// Provides methods to invoke the function from C# with argument marshalling.
/// </summary>
public class JanetFunction : JanetValue
{
    private JanetFunction(Janet value) : base(value) { }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a function.
    /// </summary>
    internal static JanetFunction Wrap(Janet value)
    {
        if (value.Type != JanetType.Function)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetFunction.");
        return new JanetFunction(value);
    }

    /// <summary>
    /// Invokes the function with the given arguments.
    /// Throws JanetException if the function signals an error.
    /// </summary>
    public Janet Invoke(params Janet[] args)
    {
        var result = Invoke(args, out var signal);
        if (signal != JanetSignal.Ok)
            throw new JanetException(result, signal);
        return result;
    }

    /// <summary>
    /// Invokes the function, returning the signal code.
    /// Does not throw on error — the caller inspects the signal.
    /// </summary>
    public unsafe Janet Invoke(Janet[] args, out JanetSignal signal)
    {
        var fnPtr = NativeMethods.shim_unwrap_function(Value.RawValue);
        int argc = args.Length;
        int status;
        long result;
        IntPtr fiber;

        if (argc == 0)
        {
            status = NativeMethods.shim_pcall(fnPtr, 0, IntPtr.Zero, out result, out fiber);
        }
        else
        {
            fixed (Janet* pArgs = args)
            {
                status = NativeMethods.shim_pcall(fnPtr, argc, (IntPtr)pArgs, out result, out fiber);
            }
        }

        signal = (JanetSignal)status;
        return new Janet(result);
    }
}
