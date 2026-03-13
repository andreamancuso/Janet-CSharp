using System.Runtime.InteropServices;

namespace JanetSharp;

/// <summary>
/// A GC-rooted wrapper around a Janet abstract value that holds a .NET object.
/// The abstract uses Janet's "sharp/object" type, storing a <see cref="GCHandle"/>
/// in the abstract data. When Janet garbage-collects the abstract, the GCHandle
/// is freed via the gc callback, releasing the .NET object.
/// </summary>
public class JanetAbstract : JanetValue
{
    // One-time initialization: register the managed GC callback with the C shim.
    private static int _initialized; // 0 = not yet, 1 = done
    private static NativeMethods.ShimAbstractGcCallback? _pinnedGcDelegate;
    private static GCHandle _delegateHandle;

    private static void EnsureInitialized()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
            return;

        _pinnedGcDelegate = OnAbstractGc;
        _delegateHandle = GCHandle.Alloc(_pinnedGcDelegate);
        var fnPtr = Marshal.GetFunctionPointerForDelegate(_pinnedGcDelegate);
        NativeMethods.shim_register_abstract_gc(fnPtr);
    }

    /// <summary>
    /// Called by the C shim's gc callback when Janet collects an abstract value.
    /// Frees the GCHandle, releasing the .NET object reference.
    /// </summary>
    private static void OnAbstractGc(IntPtr handle)
    {
        try
        {
            if (handle != IntPtr.Zero)
                GCHandle.FromIntPtr(handle).Free();
        }
        catch
        {
            // Swallow exceptions during GC finalization — nothing useful we can do.
        }
    }

    private JanetAbstract(Janet value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new Janet abstract value wrapping the given .NET object.
    /// The object is kept alive via a <see cref="GCHandle"/> stored in the abstract data.
    /// The handle is freed when Janet garbage-collects the abstract.
    /// </summary>
    /// <param name="target">The .NET object to wrap. Must not be null.</param>
    /// <returns>A GC-rooted JanetAbstract.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is null.</exception>
    public static JanetAbstract Create(object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        EnsureInitialized();

        var gcHandle = GCHandle.Alloc(target);
        var raw = NativeMethods.shim_abstract_create(GCHandle.ToIntPtr(gcHandle));
        return new JanetAbstract(new Janet(raw));
    }

    /// <summary>
    /// Wraps an existing Janet value that is known to be a sharp/object abstract.
    /// Validates that the value is indeed our custom abstract type.
    /// </summary>
    /// <param name="value">The Janet value to wrap.</param>
    /// <returns>A GC-rooted JanetAbstract.</returns>
    /// <exception cref="InvalidOperationException">The value is not a sharp/object abstract.</exception>
    internal static JanetAbstract Wrap(Janet value)
    {
        if (NativeMethods.shim_abstract_check(value.RawValue) == 0)
            throw new InvalidOperationException(
                $"Cannot wrap {value.Type} as JanetAbstract — value is not a sharp/object.");
        return new JanetAbstract(value);
    }

    /// <summary>
    /// Gets the .NET object stored in this abstract value.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This wrapper has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The GCHandle has been freed (object was collected by Janet).</exception>
    public object Target
    {
        get
        {
            var handle = NativeMethods.shim_abstract_get_handle(Value.RawValue);
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("The abstract value's GCHandle has been freed.");
            return GCHandle.FromIntPtr(handle).Target!;
        }
    }

    /// <summary>
    /// Gets the .NET object stored in this abstract value, cast to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected type of the stored object.</typeparam>
    /// <returns>The stored object cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidCastException">The stored object is not of type <typeparamref name="T"/>.</exception>
    public T GetTarget<T>() => (T)Target;

    /// <summary>
    /// Extracts the .NET object from a raw Janet value without creating a JanetAbstract wrapper.
    /// Useful in callbacks where you want to avoid allocating a wrapper object.
    /// </summary>
    /// <param name="value">A Janet value that must be a sharp/object abstract.</param>
    /// <returns>The stored .NET object.</returns>
    /// <exception cref="InvalidOperationException">The value is not a sharp/object abstract.</exception>
    public static object GetTarget(Janet value)
    {
        if (NativeMethods.shim_abstract_check(value.RawValue) == 0)
            throw new InvalidOperationException(
                $"Cannot extract target from {value.Type} — value is not a sharp/object.");
        var handle = NativeMethods.shim_abstract_get_handle(value.RawValue);
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("The abstract value's GCHandle has been freed.");
        return GCHandle.FromIntPtr(handle).Target!;
    }

    /// <summary>
    /// Extracts the .NET object from a raw Janet value, cast to <typeparamref name="T"/>.
    /// Useful in callbacks where you want to avoid allocating a wrapper object.
    /// </summary>
    /// <typeparam name="T">The expected type of the stored object.</typeparam>
    /// <param name="value">A Janet value that must be a sharp/object abstract.</param>
    /// <returns>The stored object cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">The value is not a sharp/object abstract.</exception>
    /// <exception cref="InvalidCastException">The stored object is not of type <typeparamref name="T"/>.</exception>
    public static T GetTarget<T>(Janet value) => (T)GetTarget(value);
}
