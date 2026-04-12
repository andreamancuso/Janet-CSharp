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

static Janet shim_callback_call(void *data, int32_t argc, Janet *argv) {
    Janet out = janet_wrap_nil();
    void **slot = (void **)data;
    ShimManagedCallback cb = (ShimManagedCallback)(*slot);
    if (!cb) janet_panic("managed callback pointer is null");
    int err = cb(argc, argv, &out);
    if (err) janet_panicv(out);
    return out;
}

static const JanetAbstractType shim_managed_callback_type = {
    "sharp/callback",
    NULL,  /* gc */
    NULL,  /* gcmark */
    NULL,  /* get */
    NULL,  /* put */
    NULL,  /* marshal */
    NULL,  /* unmarshal */
    NULL,  /* tostring */
    NULL,  /* compare */
    NULL,  /* hash */
    NULL,  /* next */
    shim_callback_call,
    JANET_ATEND_CALL
};

SHIM_EXPORT Janet shim_create_callback(ShimManagedCallback cb) {
    void **data = (void **)janet_abstract(&shim_managed_callback_type, sizeof(void *));
    *data = (void *)cb;
    return janet_wrap_abstract(data);
}

/* === Fibers (Coroutines) ===
 *
 * janet_continue internally uses janet_try (which wraps setjmp), so panics
 * are caught and returned as error signals — safe to call across P/Invoke.
 */

SHIM_EXPORT void *shim_fiber_new(void *fn, int32_t capacity, int32_t argc, const Janet *argv) {
    return (void *)janet_fiber((JanetFunction *)fn, capacity, argc, argv);
}

SHIM_EXPORT int shim_continue(void *fiber, Janet in, Janet *out) {
    return (int)janet_continue((JanetFiber *)fiber, in, out);
}

SHIM_EXPORT int shim_fiber_status(void *fiber) {
    return (int)janet_fiber_status((JanetFiber *)fiber);
}

SHIM_EXPORT int shim_fiber_can_resume(void *fiber) {
    return janet_fiber_can_resume((JanetFiber *)fiber);
}

SHIM_EXPORT void *shim_unwrap_fiber(Janet x) {
    return (void *)janet_unwrap_fiber(x);
}

SHIM_EXPORT Janet shim_wrap_fiber_value(void *fiber) {
    return janet_wrap_fiber((JanetFiber *)fiber);
}

/* === Custom Abstract Type ("sharp/object") ===
 *
 * A single shared abstract type that wraps a .NET GCHandle (8 bytes).
 * When Janet GCs the abstract, the gc callback invokes a managed function
 * pointer to free the GCHandle. This lets Janet's GC drive the release
 * of arbitrary C# objects.
 */

typedef void (*ShimAbstractGcCallback)(void *handle);
static ShimAbstractGcCallback shim_abstract_gc_cb = NULL;

static int shim_abstract_gc(void *data, size_t len) {
    (void)len;
    void **slot = (void **)data;
    void *handle = *slot;
    if (handle && shim_abstract_gc_cb) {
        shim_abstract_gc_cb(handle);
        *slot = NULL; /* prevent double-free */
    }
    return 0;
}

static void shim_abstract_tostring(void *data, JanetBuffer *buf) {
    (void)data;
    janet_buffer_push_cstring(buf, "<sharp/object>");
}

static const JanetAbstractType shim_abstract_type = {
    "sharp/object",
    shim_abstract_gc,
    NULL,  /* gcmark */
    NULL,  /* get */
    NULL,  /* put */
    NULL,  /* marshal */
    NULL,  /* unmarshal */
    shim_abstract_tostring,
    NULL,  /* compare */
    NULL,  /* hash */
    NULL,  /* next */
    JANET_ATEND_NEXT
};

SHIM_EXPORT void shim_register_abstract_gc(ShimAbstractGcCallback cb) {
    shim_abstract_gc_cb = cb;
}

SHIM_EXPORT Janet shim_abstract_create(void *handle) {
    void **data = (void **)janet_abstract(&shim_abstract_type, sizeof(void *));
    *data = handle;
    return janet_wrap_abstract(data);
}

SHIM_EXPORT void *shim_abstract_get_handle(Janet x) {
    void **data = (void **)janet_unwrap_abstract(x);
    return *data;
}

SHIM_EXPORT int shim_abstract_check(Janet x) {
    if (janet_type(x) != JANET_ABSTRACT) return 0;
    return janet_abstract_type(janet_unwrap_abstract(x)) == &shim_abstract_type;
}

/* === Module System Support === */

SHIM_EXPORT void *shim_make_env(void *parent) {
    JanetTable *env = janet_table(0);
    env->proto = (JanetTable *)parent;
    return (void *)env;
}

SHIM_EXPORT Janet shim_wrap_table(void *tbl) {
    return janet_wrap_table((JanetTable *)tbl);
}
