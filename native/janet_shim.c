/*
 * janet_shim.c - Safe execution wrapper for Janet ↔ .NET interop
 *
 * This shim provides a stable C ABI that the .NET P/Invoke layer calls into.
 * Its primary purpose is to ensure that Janet's setjmp/longjmp-based error
 * handling NEVER crosses a P/Invoke frame boundary (which would crash the CLR).
 *
 * All Janet API calls that could potentially longjmp are wrapped here.
 * janet_pcall internally uses setjmp to catch panics, making it safe.
 */

#include <janet.h>

/* Export macro for shared library */
#ifdef _WIN32
#define SHIM_EXPORT __declspec(dllexport)
#else
#define SHIM_EXPORT __attribute__((visibility("default")))
#endif

/* === Lifecycle === */

SHIM_EXPORT int shim_init(void) {
    return janet_init();
}

SHIM_EXPORT void shim_deinit(void) {
    janet_deinit();
}

SHIM_EXPORT void *shim_core_env(void) {
    return (void *)janet_core_env(NULL);
}

/* === Safe Execution (Critical Path) === */

/*
 * shim_pcall: The most important function in this shim.
 *
 * Janet uses setjmp/longjmp for error handling ("panics"). If a longjmp
 * unwinds through a C# P/Invoke frame, the CLR will crash with a
 * corrupted stack. janet_pcall uses setjmp internally to catch these
 * panics, converting them into a return code.
 *
 * Returns: JanetSignal enum value (0 = JANET_SIGNAL_OK, 1 = JANET_SIGNAL_ERROR)
 * On success: *out contains the return value
 * On error:   *out contains the error value (usually a string)
 */
SHIM_EXPORT int shim_pcall(
    void *fn,
    int argc,
    const Janet *argv,
    Janet *out,
    void **fiber_out)
{
    JanetFiber *fiber = NULL;
    JanetSignal status = janet_pcall(
        (JanetFunction *)fn,
        (int32_t)argc,
        argv,
        out,
        &fiber);

    if (fiber_out) {
        *fiber_out = (void *)fiber;
    }

    return (int)status;
}

/*
 * shim_dostring: Evaluate a Janet source string safely.
 *
 * This wraps janet_dostring which internally uses janet_pcall,
 * so longjmp panics are caught.
 *
 * Returns: 0 on success, non-zero on error.
 */
SHIM_EXPORT int shim_dostring(void *env, const char *str, Janet *out) {
    return janet_dostring((JanetTable *)env, str, "eval", out);
}

/* === GC Root Management === */

SHIM_EXPORT void shim_gcroot(Janet value) {
    janet_gcroot(value);
}

SHIM_EXPORT int shim_gcunroot(Janet value) {
    return janet_gcunroot(value);
}

/* === Value Wrapping / Unwrapping === */

SHIM_EXPORT Janet shim_wrap_number(double x) {
    return janet_wrap_number(x);
}

SHIM_EXPORT double shim_unwrap_number(Janet x) {
    return janet_unwrap_number(x);
}

SHIM_EXPORT Janet shim_wrap_nil(void) {
    return janet_wrap_nil();
}

SHIM_EXPORT Janet shim_wrap_boolean(int x) {
    return janet_wrap_boolean(x);
}

SHIM_EXPORT int shim_unwrap_boolean(Janet x) {
    return janet_unwrap_boolean(x);
}

SHIM_EXPORT Janet shim_wrap_integer(int32_t x) {
    return janet_wrap_number((double)x);
}

SHIM_EXPORT int32_t shim_unwrap_integer(Janet x) {
    return (int32_t)janet_unwrap_number(x);
}

/* === Type Inspection === */

SHIM_EXPORT int shim_type(Janet x) {
    return (int)janet_type(x);
}

SHIM_EXPORT int shim_truthy(Janet x) {
    return janet_truthy(x);
}

/* === Strings / Symbols / Keywords === */

SHIM_EXPORT Janet shim_wrap_string(const char *s) {
    return janet_cstringv(s);
}

SHIM_EXPORT Janet shim_wrap_symbol(const char *s) {
    return janet_csymbolv(s);
}

SHIM_EXPORT Janet shim_wrap_keyword(const char *s) {
    return janet_ckeywordv(s);
}

SHIM_EXPORT const uint8_t *shim_unwrap_string_ptr(Janet s) {
    return janet_unwrap_string(s);
}

SHIM_EXPORT int32_t shim_string_length(Janet s) {
    return janet_string_length(janet_unwrap_string(s));
}

/* === Arrays === */

SHIM_EXPORT Janet shim_array_new(int32_t capacity) {
    return janet_wrap_array(janet_array(capacity));
}

SHIM_EXPORT void shim_array_push(Janet arr, Janet val) {
    janet_array_push(janet_unwrap_array(arr), val);
}

SHIM_EXPORT Janet shim_array_pop(Janet arr) {
    return janet_array_pop(janet_unwrap_array(arr));
}

SHIM_EXPORT Janet shim_array_get(Janet arr, int32_t idx) {
    JanetArray *a = janet_unwrap_array(arr);
    if (idx < 0 || idx >= a->count) return janet_wrap_nil();
    return a->data[idx];
}

SHIM_EXPORT void shim_array_set(Janet arr, int32_t idx, Janet val) {
    JanetArray *a = janet_unwrap_array(arr);
    if (idx >= 0 && idx < a->count) {
        a->data[idx] = val;
    }
}

SHIM_EXPORT int32_t shim_array_count(Janet arr) {
    return janet_unwrap_array(arr)->count;
}

SHIM_EXPORT void shim_array_ensure(Janet arr, int32_t cap) {
    JanetArray *a = janet_unwrap_array(arr);
    janet_array_ensure(a, cap, 2);
}

/* === Tuples === */

SHIM_EXPORT Janet shim_tuple_n(const Janet *values, int32_t n) {
    return janet_wrap_tuple(janet_tuple_n(values, n));
}

SHIM_EXPORT Janet shim_tuple_get(Janet tup, int32_t idx) {
    const Janet *t = janet_unwrap_tuple(tup);
    int32_t len = janet_tuple_length(t);
    if (idx < 0 || idx >= len) return janet_wrap_nil();
    return t[idx];
}

SHIM_EXPORT int32_t shim_tuple_length(Janet tup) {
    return janet_tuple_length(janet_unwrap_tuple(tup));
}

/* === Tables === */

SHIM_EXPORT Janet shim_table_new(int32_t capacity) {
    return janet_wrap_table(janet_table(capacity));
}

SHIM_EXPORT Janet shim_table_get(Janet tbl, Janet key) {
    Janet val = janet_table_get(janet_unwrap_table(tbl), key);
    return val;
}

SHIM_EXPORT void shim_table_put(Janet tbl, Janet key, Janet val) {
    janet_table_put(janet_unwrap_table(tbl), key, val);
}

SHIM_EXPORT Janet shim_table_remove(Janet tbl, Janet key) {
    JanetTable *t = janet_unwrap_table(tbl);
    Janet old = janet_table_get(t, key);
    janet_table_remove(t, key);
    return old;
}

SHIM_EXPORT int32_t shim_table_count(Janet tbl) {
    return janet_unwrap_table(tbl)->count;
}

SHIM_EXPORT void shim_table_clear(Janet tbl) {
    JanetTable *t = janet_unwrap_table(tbl);
    /* Clear by removing all entries - reset count and zero buckets */
    for (int32_t i = 0; i < t->capacity; i++) {
        t->data[i].key = janet_wrap_nil();
        t->data[i].value = janet_wrap_nil();
    }
    t->count = 0;
    t->deleted = 0;
}

/* === Structs (immutable tables) === */

SHIM_EXPORT Janet shim_struct_get(Janet st, Janet key) {
    return janet_struct_get(janet_unwrap_struct(st), key);
}

SHIM_EXPORT int32_t shim_struct_length(Janet st) {
    return janet_struct_length(janet_unwrap_struct(st));
}

/* === Buffers === */

SHIM_EXPORT Janet shim_buffer_new(int32_t capacity) {
    return janet_wrap_buffer(janet_buffer(capacity));
}

SHIM_EXPORT void shim_buffer_push_bytes(Janet buf, const uint8_t *data, int32_t len) {
    janet_buffer_push_bytes(janet_unwrap_buffer(buf), data, len);
}

SHIM_EXPORT void shim_buffer_push_u8(Janet buf, uint8_t byte_val) {
    janet_buffer_push_u8(janet_unwrap_buffer(buf), byte_val);
}

SHIM_EXPORT int32_t shim_buffer_count(Janet buf) {
    return janet_unwrap_buffer(buf)->count;
}

SHIM_EXPORT const uint8_t *shim_buffer_data_ptr(Janet buf) {
    return janet_unwrap_buffer(buf)->data;
}

SHIM_EXPORT void shim_buffer_setcount(Janet buf, int32_t count) {
    JanetBuffer *b = janet_unwrap_buffer(buf);
    if (count >= 0 && count <= b->capacity) {
        b->count = count;
    }
}

SHIM_EXPORT void shim_buffer_ensure(Janet buf, int32_t capacity) {
    JanetBuffer *b = janet_unwrap_buffer(buf);
    janet_buffer_ensure(b, capacity, 2);
}
