using System.Runtime.InteropServices;

namespace JanetSharp;

/// <summary>
/// Wraps a C# function as a Janet-callable CFunction.
/// Uses a C-side trampoline to prevent longjmp from unwinding through managed frames.
/// Implements IDisposable to unregister the callback slot and release the pinned delegate.
/// </summary>
public sealed class JanetCallback : IDisposable
{
    /// <summary>
    /// The user-facing callback signature.
    /// Takes a read-only span of Janet arguments, returns a Janet value.
    /// </summary>
    public delegate Janet CallbackFunc(ReadOnlySpan<Janet> args);

    private readonly CallbackFunc _userCallback;
    private readonly NativeMethods.ShimManagedCallback _nativeDelegate;
    private readonly GCHandle _delegateHandle;
    private readonly int _slot;
    private bool _disposed;

    /// <summary>
    /// The Janet CFunction value representing this callback.
    /// Can be registered in a Janet environment or passed to Janet code.
    /// </summary>
    public Janet Value { get; }

    /// <summary>
    /// Creates a new callback wrapping the given C# function.
    /// </summary>
    public JanetCallback(CallbackFunc callback)
    {
        _userCallback = callback ?? throw new ArgumentNullException(nameof(callback));

        // Create the native delegate that bridges to the user callback
        _nativeDelegate = NativeTrampoline;

        // Pin the delegate so GC doesn't collect it while Janet holds the function pointer
        _delegateHandle = GCHandle.Alloc(_nativeDelegate);

        // Get the unmanaged function pointer for the delegate
        var fnPtr = Marshal.GetFunctionPointerForDelegate(_nativeDelegate);

        // Register with the C-side trampoline table
        _slot = NativeMethods.shim_register_callback(fnPtr);
        if (_slot < 0)
        {
            _delegateHandle.Free();
            throw new InvalidOperationException(
                "Maximum number of Janet callbacks (64) reached. Dispose unused callbacks to free slots.");
        }

        // Get the Janet CFunction value for this slot's trampoline
        Value = new Janet(NativeMethods.shim_wrap_callback(_slot));
    }

    /// <summary>
    /// Called by the C trampoline. Catches all C# exceptions and converts them
    /// to error codes that the C trampoline turns into janet_panicv calls.
    /// </summary>
    private unsafe int NativeTrampoline(int argc, IntPtr argv, out long result)
    {
        try
        {
            ReadOnlySpan<Janet> args = argc > 0
                ? new ReadOnlySpan<Janet>((void*)argv, argc)
                : ReadOnlySpan<Janet>.Empty;

            Janet returnValue = _userCallback(args);
            result = returnValue.RawValue;
            return 0; // success
        }
        catch (Exception ex)
        {
            // Convert the exception to a Janet error string.
            // The C trampoline will call janet_panicv(out) with this value.
            result = Janet.From(ex.Message).RawValue;
            return 1; // error
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        NativeMethods.shim_unregister_callback(_slot);
        _delegateHandle.Free();
    }
}
