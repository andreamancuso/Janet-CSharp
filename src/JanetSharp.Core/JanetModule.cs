namespace JanetSharp;

/// <summary>
/// Provides methods for integrating with Janet's native module system.
/// Allows registering modules from source code or pre-built tables
/// that are discoverable via Janet's <c>(import name)</c> syntax.
/// </summary>
/// <remarks>
/// Module names must be simple identifiers (no '/', '.', or '@' prefix)
/// to work with Janet's preload path.
/// </remarks>
public sealed class JanetModule
{
    private readonly JanetRuntime _runtime;
    private readonly List<JanetCallback> _registeredLoaders = new();

    internal JanetModule(JanetRuntime runtime)
    {
        _runtime = runtime;
    }

    /// <summary>
    /// Registers a module from Janet source code. The source is evaluated
    /// in an isolated child environment, and the result is cached so that
    /// <c>(import name)</c> works from Janet code.
    /// </summary>
    /// <param name="name">The module name (simple identifier, no '/', '.', or '@').</param>
    /// <param name="source">Janet source code defining the module's exports.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> contains invalid characters.</exception>
    /// <exception cref="JanetException">The source code produced an error during evaluation.</exception>
    public void AddModule(string name, string source)
    {
        ValidateModuleName(name);

        // Create a child environment with CoreEnvironment as prototype
        IntPtr childEnvPtr = NativeMethods.shim_make_env(_runtime.CoreEnvironment);

        // Evaluate the module source in the child environment
        int status = NativeMethods.shim_dostring(childEnvPtr, source, out long result);
        if (status != 0)
            throw new JanetException(new Janet(result), (JanetSignal)status);

        // Wrap child env pointer as a NaN-boxed Janet value
        long envJanet = NativeMethods.shim_wrap_table(childEnvPtr);

        // Cache the module environment
        CacheModule(name, envJanet);

        // GC-root the environment so it survives for the runtime's lifetime
        NativeMethods.shim_gcroot(envJanet);
    }

    /// <summary>
    /// Registers a pre-built <see cref="JanetTable"/> as a module in the cache.
    /// The table must use Janet's module environment format (symbol keys mapping
    /// to <c>{:value val}</c> structs), as produced by <c>(defn ...)</c> and similar forms.
    /// </summary>
    /// <param name="name">The module name (simple identifier, no '/', '.', or '@').</param>
    /// <param name="environment">A JanetTable containing the module's bindings.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> contains invalid characters.</exception>
    public void AddModule(string name, JanetTable environment)
    {
        ValidateModuleName(name);
        CacheModule(name, environment.Value.RawValue);
    }

    /// <summary>
    /// Registers a custom loader function in Janet's <c>module/loaders</c> table.
    /// The loader receives <c>(path args)</c> and should return a module environment table.
    /// A matching entry in <c>module/paths</c> is needed for the loader to be invoked.
    /// </summary>
    /// <param name="keyword">The loader keyword (e.g., "csharp").</param>
    /// <param name="loader">The callback function that loads modules.</param>
    /// <returns>The JanetCallback wrapping the loader.</returns>
    public JanetCallback RegisterLoader(string keyword, JanetCallback.CallbackFunc loader)
    {
        var cb = new JanetCallback(loader);

        // Get module/loaders table
        int status = NativeMethods.shim_dostring(
            _runtime.CoreEnvironment, "module/loaders", out long loadersRaw);
        if (status != 0)
            throw new JanetException("Failed to access module/loaders");

        // Put keyword -> loader into module/loaders
        long keyJanet = NativeMethods.shim_wrap_keyword(keyword);
        NativeMethods.shim_table_put(loadersRaw, keyJanet, cb.Value.RawValue);

        // Keep callback reference alive
        _registeredLoaders.Add(cb);

        return cb;
    }

    /// <summary>
    /// Checks whether a module with the given name exists in <c>module/cache</c>.
    /// </summary>
    /// <param name="name">The module name to check.</param>
    /// <returns><c>true</c> if the module is cached; otherwise <c>false</c>.</returns>
    public bool IsModuleCached(string name)
    {
        long cacheRaw = GetModuleCache();
        long result = NativeMethods.shim_table_get(cacheRaw, Janet.From(name).RawValue);
        return new Janet(result).Type != JanetType.Nil;
    }

    private long GetModuleCache()
    {
        int status = NativeMethods.shim_dostring(
            _runtime.CoreEnvironment, "module/cache", out long cacheRaw);
        if (status != 0)
            throw new JanetException("Failed to access module/cache");
        return cacheRaw;
    }

    private void CacheModule(string name, long envJanet)
    {
        long cacheRaw = GetModuleCache();
        NativeMethods.shim_table_put(cacheRaw, Janet.From(name).RawValue, envJanet);
    }

    private static void ValidateModuleName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Contains('/') || name.Contains('.') || name.StartsWith('@'))
            throw new ArgumentException(
                "Module name must be a simple identifier without '/', '.', or '@' prefix. " +
                "These characters prevent Janet's preload path from resolving the module.",
                nameof(name));
    }

    internal void DisposeLoaders()
    {
        foreach (var cb in _registeredLoaders)
            cb.Dispose();
        _registeredLoaders.Clear();
    }
}
