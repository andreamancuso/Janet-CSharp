namespace JanetSharp;

/// <summary>
/// Manages the Janet VM lifecycle. Janet is process-global and single-threaded,
/// so only one instance may exist at a time.
/// </summary>
public sealed class JanetRuntime : IDisposable
{
    [ThreadStatic]
    internal static JanetRuntime? t_activeRuntime;
    
    private static int _totalActiveRuntimes;
    private static int _generation;
    private readonly int _ownerThreadId;
    private bool _disposed;
    private JanetModule? _modules;
    private readonly System.Collections.Concurrent.ConcurrentQueue<long> _pendingUnroots = new();

    /// <summary>
    /// Returns true if any JanetRuntime instance is currently active (VM initialized).
    /// Used by JanetValue finalizers to avoid calling into a deinitialized VM.
    /// </summary>
    internal static bool IsActive => Volatile.Read(ref _totalActiveRuntimes) > 0;

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
    /// Initializes the Janet VM. Throws if another instance is already active on this thread.
    /// </summary>
    public JanetRuntime()
    {
        if (t_activeRuntime != null)
            throw new InvalidOperationException(
                "A JanetRuntime instance is already active on this thread. Only one runtime may exist per thread.");

        _ownerThreadId = Environment.CurrentManagedThreadId;

        int result = NativeMethods.shim_init();
        if (result != 0)
        {
            throw new JanetException($"janet_init failed with code {result}");
        }

        t_activeRuntime = this;
        Interlocked.Increment(ref _totalActiveRuntimes);
        Interlocked.Increment(ref _generation);
        CoreEnvironment = NativeMethods.shim_core_env();
    }

    internal void EnqueueDeferredUnroot(long rawValue)
    {
        _pendingUnroots.Enqueue(rawValue);
    }

    internal void ProcessDeferredUnroots()
    {
        while (_pendingUnroots.TryDequeue(out long rawValue))
        {
            NativeMethods.shim_gcunroot(rawValue);
        }
    }

    /// <summary>
    /// Evaluates a Janet source string and returns the result.
    /// Throws <see cref="JanetException"/> if evaluation produces an error signal.
    /// </summary>
    /// <param name="code">The Janet source code to evaluate.</param>
    /// <returns>The result of evaluating the expression.</returns>
    /// <exception cref="JanetException">The evaluation produced an error signal.</exception>
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
    /// <param name="code">The Janet source code to evaluate.</param>
    /// <param name="signal">Receives the signal code from evaluation.</param>
    /// <returns>The result of evaluating the expression.</returns>
    public Janet Eval(string code, out JanetSignal signal)
    {
        CheckThread();
        CheckDisposed();
        ProcessDeferredUnroots();

        int status = NativeMethods.shim_dostring(CoreEnvironment, code, out long rawResult);
        signal = (JanetSignal)status;
        return new Janet(rawResult);
    }

    /// <summary>
    /// Evaluates a Janet expression and returns the result as a JanetFunction.
    /// Throws if the expression does not evaluate to a function.
    /// </summary>
    /// <param name="expression">A Janet expression that evaluates to a function.</param>
    /// <returns>A GC-rooted JanetFunction wrapper.</returns>
    /// <exception cref="JanetException">The expression produced an error signal.</exception>
    /// <exception cref="InvalidOperationException">The expression did not evaluate to a function.</exception>
    public JanetFunction GetFunction(string expression)
    {
        var result = Eval(expression);
        return result.AsFunction();
    }

    /// <summary>
    /// Provides access to Janet's native module system for registering
    /// modules that can be imported via <c>(import name)</c>.
    /// </summary>
    public JanetModule Modules
    {
        get
        {
            CheckThread();
            CheckDisposed();
            return _modules ??= new JanetModule(this);
        }
    }

    /// <summary>
    /// Registers a C# callback as a named function in the Janet environment.
    /// The returned JanetCallback must be kept alive (not disposed) as long as
    /// Janet code may call the function.
    /// </summary>
    /// <param name="name">The name to register the function under in the Janet environment.</param>
    /// <param name="callback">The C# callback function to expose to Janet.</param>
    /// <returns>A JanetCallback that must be kept alive while the function is in use.</returns>
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose module system loader callbacks before tearing down the VM
        _modules?.DisposeLoaders();

        // Flush pending finalizers so JanetValue objects that are already unreachable
        // get their shim_gcunroot calls while the VM is still alive.
        GC.Collect();
        GC.WaitForPendingFinalizers();

        ProcessDeferredUnroots();

        NativeMethods.shim_deinit();
        t_activeRuntime = null;
        Interlocked.Decrement(ref _totalActiveRuntimes);
    }
}
