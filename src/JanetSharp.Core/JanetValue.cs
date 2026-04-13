using System.Runtime.CompilerServices;

namespace JanetSharp;

/// <summary>
/// A GC-safe handle to a Janet value. Roots the value in Janet's garbage collector
/// on creation and unroots on disposal, preventing the Janet GC from collecting
/// values that C# still references.
///
/// Only reference types (strings, arrays, tables, functions, etc.) need GC rooting.
/// Primitives (numbers, nil, booleans) are encoded entirely in the NaN-box and
/// are not tracked by Janet's GC.
/// </summary>
public class JanetValue : IDisposable
{
    // Deduplication table: ensures that multiple C# references to the same
    // Janet GC object share a single GC root, preventing double-root/unroot bugs.
    private static readonly ConditionalWeakTable<object, JanetValue> _rootedValues = new();

    // Box the raw value as a lookup key for the ConditionalWeakTable.
    // We use the raw long directly since ConditionalWeakTable needs reference-type keys.
    private sealed class RootKey
    {
        public readonly long RawValue;
        public RootKey(long raw) => RawValue = raw;

        public override bool Equals(object? obj) => obj is RootKey other && RawValue == other.RawValue;
        public override int GetHashCode() => RawValue.GetHashCode();
    }

    private readonly Janet _value;
    private readonly bool _isRooted;
    private readonly int _generation; // which runtime generation created this value
    private readonly JanetRuntime? _runtime; // the runtime that created this value
    private int _disposed; // 0 = alive, 1 = disposed (for thread-safe CAS)

    /// <summary>
    /// The underlying Janet value.
    /// </summary>
    public Janet Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            return _value;
        }
    }

    /// <summary>
    /// Implicitly converts a JanetValue subclass to the raw Janet struct.
    /// </summary>
    public static implicit operator Janet(JanetValue value) => value.Value;

    /// <summary>
    /// Creates a GC-safe handle to a Janet value.
    /// Reference types are rooted in Janet's GC; primitives are not.
    /// </summary>
    public JanetValue(Janet value)
    {
        _value = value;
        _isRooted = value.IsGcType;
        _generation = JanetRuntime.Generation;
        _runtime = JanetRuntime.t_activeRuntime;

        if (_isRooted)
        {
            NativeMethods.shim_gcroot(value.RawValue);
        }
    }

    /// <summary>
    /// The Janet type of the wrapped value.
    /// </summary>
    public JanetType Type => _value.Type;

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the GC root for this value. Safe to call multiple times.
    /// </summary>
    /// <param name="disposing">True if called from Dispose; false if called from the finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        // Thread-safe: only the first call unroots
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        // Only unroot if the SAME runtime that created this value is still active.
        if (_isRooted && JanetRuntime.IsActive && JanetRuntime.Generation == _generation && _runtime != null)
        {
            if (disposing)
            {
                // Manual Dispose(): safe to unroot synchronously
                NativeMethods.shim_gcunroot(_value.RawValue);
            }
            else
            {
                // Finalizer Thread: deferred unrooting to avoid crashing the Janet thread
                _runtime.EnqueueDeferredUnroot(_value.RawValue);
            }
        }
    }

    ~JanetValue()
    {
        Dispose(disposing: false);
    }

    /// <inheritdoc />
    public override string ToString() => _value.ToString();
}
