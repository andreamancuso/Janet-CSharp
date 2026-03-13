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

    // === Strings / Symbols / Keywords ===

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial long shim_wrap_string(string s);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial long shim_wrap_symbol(string s);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial long shim_wrap_keyword(string s);

    [LibraryImport(LibName)]
    internal static partial IntPtr shim_unwrap_string_ptr(long s);

    [LibraryImport(LibName)]
    internal static partial int shim_string_length(long s);

    // === Arrays ===

    [LibraryImport(LibName)]
    internal static partial long shim_array_new(int capacity);

    [LibraryImport(LibName)]
    internal static partial void shim_array_push(long arr, long val);

    [LibraryImport(LibName)]
    internal static partial long shim_array_pop(long arr);

    [LibraryImport(LibName)]
    internal static partial long shim_array_get(long arr, int idx);

    [LibraryImport(LibName)]
    internal static partial void shim_array_set(long arr, int idx, long val);

    [LibraryImport(LibName)]
    internal static partial int shim_array_count(long arr);

    [LibraryImport(LibName)]
    internal static partial void shim_array_ensure(long arr, int cap);

    // === Tuples ===

    [LibraryImport(LibName)]
    internal static partial long shim_tuple_n(IntPtr values, int n);

    [LibraryImport(LibName)]
    internal static partial long shim_tuple_get(long tup, int idx);

    [LibraryImport(LibName)]
    internal static partial int shim_tuple_length(long tup);

    // === Tables ===

    [LibraryImport(LibName)]
    internal static partial long shim_table_new(int capacity);

    [LibraryImport(LibName)]
    internal static partial long shim_table_get(long tbl, long key);

    [LibraryImport(LibName)]
    internal static partial void shim_table_put(long tbl, long key, long val);

    [LibraryImport(LibName)]
    internal static partial long shim_table_remove(long tbl, long key);

    [LibraryImport(LibName)]
    internal static partial int shim_table_count(long tbl);

    [LibraryImport(LibName)]
    internal static partial void shim_table_clear(long tbl);

    // === Structs (immutable tables) ===

    [LibraryImport(LibName)]
    internal static partial long shim_struct_get(long st, long key);

    [LibraryImport(LibName)]
    internal static partial int shim_struct_length(long st);

    // === Dictionary Iteration (Tables & Structs) ===

    [LibraryImport(LibName)]
    internal static partial int shim_dictionary_collect(long dict, long[] keysOut, long[] valuesOut, int maxCount);

    // === Buffers ===

    [LibraryImport(LibName)]
    internal static partial long shim_buffer_new(int capacity);

    [LibraryImport(LibName)]
    internal static partial void shim_buffer_push_bytes(long buf, IntPtr data, int len);

    [LibraryImport(LibName)]
    internal static partial void shim_buffer_push_u8(long buf, byte byteVal);

    [LibraryImport(LibName)]
    internal static partial int shim_buffer_count(long buf);

    [LibraryImport(LibName)]
    internal static partial IntPtr shim_buffer_data_ptr(long buf);

    [LibraryImport(LibName)]
    internal static partial void shim_buffer_setcount(long buf, int count);

    [LibraryImport(LibName)]
    internal static partial void shim_buffer_ensure(long buf, int capacity);

    // === Functions ===

    [LibraryImport(LibName)]
    internal static partial IntPtr shim_unwrap_function(long x);

    // === Environment Definition ===

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void shim_def(IntPtr env, string name, long val);

    // === Callback System ===

    /// <summary>
    /// Managed callback delegate called by C trampolines.
    /// Returns 0 on success, non-zero on error.
    /// On error, result contains the error value for janet_panicv.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ShimManagedCallback(int argc, IntPtr argv, out long result);

    [LibraryImport(LibName)]
    internal static partial int shim_register_callback(IntPtr cb);

    [LibraryImport(LibName)]
    internal static partial void shim_unregister_callback(int slot);

    [LibraryImport(LibName)]
    internal static partial long shim_wrap_callback(int slot);

    // === Fibers (Coroutines) ===

    [LibraryImport(LibName)]
    internal static partial IntPtr shim_fiber_new(IntPtr fn, int capacity, int argc, IntPtr argv);

    [LibraryImport(LibName)]
    internal static partial int shim_continue(IntPtr fiber, long inValue, out long outValue);

    [LibraryImport(LibName)]
    internal static partial int shim_fiber_status(IntPtr fiber);

    [LibraryImport(LibName)]
    internal static partial int shim_fiber_can_resume(IntPtr fiber);

    [LibraryImport(LibName)]
    internal static partial IntPtr shim_unwrap_fiber(long x);

    [LibraryImport(LibName)]
    internal static partial long shim_wrap_fiber_value(IntPtr fiber);
}
