using System.Runtime.InteropServices;

namespace JanetSharp;

/// <summary>
/// P/Invoke bindings to the janet_shim native library.
/// Janet values are 64-bit NaN-boxed unions, marshalled via the Janet struct (8 bytes).
/// </summary>
internal static partial class NativeMethods
{
    const string LibName = "janet_shim";

    // === Lifecycle ===

    [LibraryImport(LibName)]
    internal static partial int shim_init();

    [LibraryImport(LibName)]
    internal static partial void shim_deinit();

    [LibraryImport(LibName)]
    internal static partial IntPtr shim_core_env();

    // === Safe Execution ===

    [LibraryImport(LibName)]
    internal static partial int shim_pcall(
        IntPtr fn,
        int argc,
        IntPtr argv,
        out long result,
        out IntPtr fiber);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int shim_dostring(
        IntPtr env,
        string str,
        out long result);

    // === GC Root Management ===

    [LibraryImport(LibName)]
    internal static partial void shim_gcroot(long value);

    [LibraryImport(LibName)]
    internal static partial int shim_gcunroot(long value);

    // === Value Wrapping ===

    [LibraryImport(LibName)]
    internal static partial long shim_wrap_number(double x);

    [LibraryImport(LibName)]
    internal static partial double shim_unwrap_number(long x);

    [LibraryImport(LibName)]
    internal static partial long shim_wrap_nil();

    [LibraryImport(LibName)]
    internal static partial long shim_wrap_boolean(int x);

    [LibraryImport(LibName)]
    internal static partial int shim_unwrap_boolean(long x);

    [LibraryImport(LibName)]
    internal static partial long shim_wrap_integer(int value);

    [LibraryImport(LibName)]
    internal static partial int shim_unwrap_integer(long x);

    // === Type Inspection ===

    [LibraryImport(LibName)]
    internal static partial int shim_type(long x);

    [LibraryImport(LibName)]
    internal static partial int shim_truthy(long x);
}
