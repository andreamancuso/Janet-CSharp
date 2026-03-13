namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet fiber (coroutine).
/// Provides methods to create fibers, resume them, and inspect their status.
/// </summary>
public class JanetFiber : JanetValue
{
    private readonly IntPtr _fiberPtr;

    private JanetFiber(Janet value, IntPtr fiberPtr) : base(value)
    {
        _fiberPtr = fiberPtr;
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a fiber.
    /// </summary>
    internal static JanetFiber Wrap(Janet value)
    {
        if (value.Type != JanetType.Fiber)
            throw new InvalidOperationException($"Cannot wrap {value.Type} as JanetFiber.");
        var ptr = NativeMethods.shim_unwrap_fiber(value.RawValue);
        return new JanetFiber(value, ptr);
    }

    /// <summary>
    /// Creates a new fiber from a JanetFunction with optional arguments.
    /// The fiber starts in the New status and must be resumed to begin execution.
    /// </summary>
    /// <param name="fn">The function to execute as a fiber.</param>
    /// <param name="args">Optional arguments passed to the function on first resume.</param>
    /// <returns>A GC-rooted JanetFiber.</returns>
    public static unsafe JanetFiber Create(JanetFunction fn, params Janet[] args)
    {
        var fnPtr = NativeMethods.shim_unwrap_function(fn.Value.RawValue);
        IntPtr fiberPtr;

        if (args.Length == 0)
        {
            fiberPtr = NativeMethods.shim_fiber_new(fnPtr, 64, 0, IntPtr.Zero);
        }
        else
        {
            fixed (Janet* pArgs = args)
            {
                fiberPtr = NativeMethods.shim_fiber_new(fnPtr, 64, args.Length, (IntPtr)pArgs);
            }
        }

        var janetValue = new Janet(NativeMethods.shim_wrap_fiber_value(fiberPtr));
        return new JanetFiber(janetValue, fiberPtr);
    }

    /// <summary>
    /// Resumes the fiber with nil as the input value.
    /// Throws <see cref="JanetException"/> if the fiber signals an error.
    /// </summary>
    /// <returns>The value yielded or returned by the fiber.</returns>
    /// <exception cref="JanetException">The fiber signaled an error.</exception>
    public Janet Resume() => Resume(Janet.Nil);

    /// <summary>
    /// Resumes the fiber with an input value.
    /// Throws <see cref="JanetException"/> if the fiber signals an error.
    /// </summary>
    /// <param name="value">The value to send into the fiber.</param>
    /// <returns>The value yielded or returned by the fiber.</returns>
    /// <exception cref="JanetException">The fiber signaled an error.</exception>
    public Janet Resume(Janet value)
    {
        var result = Resume(value, out var signal);
        if (signal == JanetSignal.Error)
            throw new JanetException(result, signal);
        return result;
    }

    /// <summary>
    /// Resumes the fiber with an input value, returning the signal code.
    /// Does not throw on error — the caller inspects the signal.
    /// </summary>
    /// <param name="value">The value to send into the fiber.</param>
    /// <param name="signal">Receives the signal code (Ok, Yield, Error, etc.).</param>
    /// <returns>The value yielded or returned by the fiber.</returns>
    public Janet Resume(Janet value, out JanetSignal signal)
    {
        // Access Value property to trigger disposed check
        _ = Value;

        int status = NativeMethods.shim_continue(_fiberPtr, value.RawValue, out long outValue);
        signal = (JanetSignal)status;
        return new Janet(outValue);
    }

    /// <summary>
    /// Gets the current status of the fiber.
    /// </summary>
    public JanetFiberStatus Status
    {
        get
        {
            _ = Value;
            return (JanetFiberStatus)NativeMethods.shim_fiber_status(_fiberPtr);
        }
    }

    /// <summary>
    /// Returns true if the fiber can be resumed (status is New, Pending, or Debug).
    /// </summary>
    public bool CanResume
    {
        get
        {
            _ = Value;
            return NativeMethods.shim_fiber_can_resume(_fiberPtr) != 0;
        }
    }
}
