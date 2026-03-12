namespace JanetSharp;

/// <summary>
/// Manages the Janet VM lifecycle. Janet is process-global and single-threaded,
/// so only one instance may exist at a time.
/// </summary>
public sealed class JanetRuntime : IDisposable
{
    private static int _instanceCount;
    private static int _generation;
    private readonly int _ownerThreadId;
    private bool _disposed;

    /// <summary>
    /// Returns true if a JanetRuntime instance is currently active (VM initialized).
    /// Used by JanetValue finalizers to avoid calling into a deinitialized VM.
    /// </summary>
    internal static bool IsActive => Volatile.Read(ref _instanceCount) != 0;

    /// <summary>
    /// Monotonically increasing generation counter. Incremented each time a new
    /// runtime is created. JanetValue records its creation generation to avoid
    /// calling shim_gcunroot on a different runtime's VM.
    /// </summary>
    internal static int Generation => Volatile.Read(ref _generation);

    /// <summary>
    /// The core Janet environment containing built-in functions.
    /// </summary>
    public IntPtr CoreEnvironment { get; }

    /// <summary>
    /// Initializes the Janet VM. Throws if another instance is already active.
    /// </summary>
    public JanetRuntime()
    {
        if (Interlocked.CompareExchange(ref _instanceCount, 1, 0) != 0)
            throw new InvalidOperationException(
                "A JanetRuntime instance is already active. Janet is process-global; only one runtime may exist at a time.");

        _ownerThreadId = Environment.CurrentManagedThreadId;

        int result = NativeMethods.shim_init();
        if (result != 0)
        {
            Interlocked.Exchange(ref _instanceCount, 0);
            throw new JanetException($"janet_init failed with code {result}");
        }

        Interlocked.Increment(ref _generation);
        CoreEnvironment = NativeMethods.shim_core_env();
    }

    /// <summary>
    /// Evaluates a Janet source string and returns the result.
    /// Throws <see cref="JanetException"/> if evaluation produces an error signal.
    /// </summary>
    public Janet Eval(string code)
    {
        var result = Eval(code, out var signal);
        if (signal != JanetSignal.Ok)
            throw new JanetException(result, signal);
        return result;
    }

    /// <summary>
    /// Evaluates a Janet source string, returning the result and signal.
    /// Does not throw on error — the caller inspects the signal.
    /// </summary>
    public Janet Eval(string code, out JanetSignal signal)
    {
        CheckThread();
        CheckDisposed();

        int status = NativeMethods.shim_dostring(CoreEnvironment, code, out long rawResult);
        signal = (JanetSignal)status;
        return new Janet(rawResult);
    }

    /// <summary>
    /// Evaluates a Janet expression and returns the result as a JanetFunction.
    /// Throws if the expression does not evaluate to a function.
    /// </summary>
    public JanetFunction GetFunction(string expression)
    {
        var result = Eval(expression);
        return result.AsFunction();
    }

    /// <summary>
    /// Registers a C# callback as a named function in the Janet environment.
    /// The returned JanetCallback must be kept alive (not disposed) as long as
    /// Janet code may call the function.
    /// </summary>
    public JanetCallback Register(string name, JanetCallback.CallbackFunc callback)
    {
        CheckThread();
        CheckDisposed();

        var cb = new JanetCallback(callback);
        NativeMethods.shim_def(CoreEnvironment, name, cb.Value.RawValue);
        return cb;
    }

    private void CheckThread()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadId)
            throw new InvalidOperationException(
                $"JanetRuntime must be accessed from the thread that created it (thread {_ownerThreadId}). " +
                $"Current thread: {Environment.CurrentManagedThreadId}. Janet is not thread-safe.");
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Flush pending finalizers so JanetValue objects that are already unreachable
        // get their shim_gcunroot calls while the VM is still alive.
        GC.Collect();
        GC.WaitForPendingFinalizers();

        NativeMethods.shim_deinit();
        Interlocked.Exchange(ref _instanceCount, 0);
    }
}
