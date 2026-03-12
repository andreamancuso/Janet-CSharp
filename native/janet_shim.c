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

/* === Dictionary Iteration (Tables & Structs) === */

/*
 * shim_dictionary_collect: Collect all key-value pairs from a table or struct.
 *
 * Uses janet_dictionary_view + janet_dictionary_next to iterate the backing
 * hash map and write raw Janet values into pre-allocated arrays.
 * Works for both JanetTable and JanetStruct types.
 *
 * Returns: number of entries written.
 */
SHIM_EXPORT int32_t shim_dictionary_collect(Janet dict, Janet *keys_out, Janet *values_out, int32_t max_count) {
    const JanetKV *kvs = NULL;
    int32_t len = 0, cap = 0;

    if (!janet_dictionary_view(dict, &kvs, &len, &cap))
        return 0;

    const JanetKV *kv = NULL;
    int32_t written = 0;

    while ((kv = janet_dictionary_next(kvs, cap, kv)) != NULL && written < max_count) {
        keys_out[written] = kv->key;
        values_out[written] = kv->value;
        written++;
    }

    return written;
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

/* === Functions === */

SHIM_EXPORT void *shim_unwrap_function(Janet x) {
    return (void *)janet_unwrap_function(x);
}

/* === Environment Definition === */

SHIM_EXPORT void shim_def(void *env, const char *name, Janet val) {
    janet_def((JanetTable *)env, name, val, NULL);
}

/* === Callback Trampoline System ===
 *
 * Janet's JanetCFunction signature has no user-data parameter, and longjmp
 * must never unwind through a managed (.NET) frame. We solve both problems
 * with a static table of C trampoline functions.
 *
 * Each trampoline:
 *   1. Calls the registered C# function pointer (which catches all .NET exceptions)
 *   2. If the C# side returns an error code, calls janet_panicv() from C (safe)
 *
 * The C# delegate signature (ShimManagedCallback):
 *   int callback(int32_t argc, const Janet *argv, Janet *out)
 *   Returns 0 on success, non-zero on error.
 *   On error, *out contains the error value for janet_panicv.
 */

typedef int (*ShimManagedCallback)(int32_t argc, const Janet *argv, Janet *out);

#define SHIM_MAX_CALLBACKS 64

static ShimManagedCallback shim_callbacks[SHIM_MAX_CALLBACKS];

#define TRAMP(N) \
    static Janet shim_tramp_##N(int32_t argc, Janet *argv) { \
        Janet out = janet_wrap_nil(); \
        if (!shim_callbacks[N]) janet_panic("callback slot " #N " is empty"); \
        int err = shim_callbacks[N](argc, argv, &out); \
        if (err) janet_panicv(out); \
        return out; \
    }

TRAMP(0)  TRAMP(1)  TRAMP(2)  TRAMP(3)  TRAMP(4)  TRAMP(5)  TRAMP(6)  TRAMP(7)
TRAMP(8)  TRAMP(9)  TRAMP(10) TRAMP(11) TRAMP(12) TRAMP(13) TRAMP(14) TRAMP(15)
TRAMP(16) TRAMP(17) TRAMP(18) TRAMP(19) TRAMP(20) TRAMP(21) TRAMP(22) TRAMP(23)
TRAMP(24) TRAMP(25) TRAMP(26) TRAMP(27) TRAMP(28) TRAMP(29) TRAMP(30) TRAMP(31)
TRAMP(32) TRAMP(33) TRAMP(34) TRAMP(35) TRAMP(36) TRAMP(37) TRAMP(38) TRAMP(39)
TRAMP(40) TRAMP(41) TRAMP(42) TRAMP(43) TRAMP(44) TRAMP(45) TRAMP(46) TRAMP(47)
TRAMP(48) TRAMP(49) TRAMP(50) TRAMP(51) TRAMP(52) TRAMP(53) TRAMP(54) TRAMP(55)
TRAMP(56) TRAMP(57) TRAMP(58) TRAMP(59) TRAMP(60) TRAMP(61) TRAMP(62) TRAMP(63)

static JanetCFunction shim_tramp_table[SHIM_MAX_CALLBACKS] = {
    shim_tramp_0,  shim_tramp_1,  shim_tramp_2,  shim_tramp_3,
    shim_tramp_4,  shim_tramp_5,  shim_tramp_6,  shim_tramp_7,
    shim_tramp_8,  shim_tramp_9,  shim_tramp_10, shim_tramp_11,
    shim_tramp_12, shim_tramp_13, shim_tramp_14, shim_tramp_15,
    shim_tramp_16, shim_tramp_17, shim_tramp_18, shim_tramp_19,
    shim_tramp_20, shim_tramp_21, shim_tramp_22, shim_tramp_23,
    shim_tramp_24, shim_tramp_25, shim_tramp_26, shim_tramp_27,
    shim_tramp_28, shim_tramp_29, shim_tramp_30, shim_tramp_31,
    shim_tramp_32, shim_tramp_33, shim_tramp_34, shim_tramp_35,
    shim_tramp_36, shim_tramp_37, shim_tramp_38, shim_tramp_39,
    shim_tramp_40, shim_tramp_41, shim_tramp_42, shim_tramp_43,
    shim_tramp_44, shim_tramp_45, shim_tramp_46, shim_tramp_47,
    shim_tramp_48, shim_tramp_49, shim_tramp_50, shim_tramp_51,
    shim_tramp_52, shim_tramp_53, shim_tramp_54, shim_tramp_55,
    shim_tramp_56, shim_tramp_57, shim_tramp_58, shim_tramp_59,
    shim_tramp_60, shim_tramp_61, shim_tramp_62, shim_tramp_63,
};

SHIM_EXPORT int shim_register_callback(ShimManagedCallback cb) {
    for (int i = 0; i < SHIM_MAX_CALLBACKS; i++) {
        if (shim_callbacks[i] == NULL) {
            shim_callbacks[i] = cb;
            return i;
        }
    }
    return -1;
}

SHIM_EXPORT void shim_unregister_callback(int slot) {
    if (slot >= 0 && slot < SHIM_MAX_CALLBACKS) {
        shim_callbacks[slot] = NULL;
    }
}

SHIM_EXPORT Janet shim_wrap_callback(int slot) {
    if (slot < 0 || slot >= SHIM_MAX_CALLBACKS) return janet_wrap_nil();
    return janet_wrap_cfunction(shim_tramp_table[slot]);
}
