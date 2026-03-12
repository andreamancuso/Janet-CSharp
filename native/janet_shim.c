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
