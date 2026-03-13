using Xunit;
using JanetSharp;

namespace JanetSharp.Tests;

// === Module System Tests (Phase 10.3) ===
// These tests verify that C#-registered modules are discoverable
// via Janet's native (import) syntax.

public class ModuleSystemTests : IDisposable
{
    private readonly JanetRuntime _runtime;

    public ModuleSystemTests() => _runtime = new JanetRuntime();
    public void Dispose() => _runtime.Dispose();

    // --- AddModule from source ---

    [Fact]
    public void AddModule_FromSource_ThenImport()
    {
        _runtime.Modules.AddModule("mymath", @"
            (def pi 3.14159)
            (defn double [x] (* x 2))
        ");
        var result = _runtime.Eval(@"
            (import mymath)
            (mymath/double 21)
        ");
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void AddModule_MultipleDefinitions()
    {
        _runtime.Modules.AddModule("utils", @"
            (defn add [a b] (+ a b))
            (defn mul [a b] (* a b))
            (defn sub [a b] (- a b))
        ");
        var result = _runtime.Eval(@"
            (import utils)
            (utils/add (utils/mul 3 4) (utils/sub 10 5))
        ");
        Assert.Equal(17.0, result.AsNumber());
    }

    [Fact]
    public void AddModule_DoesNotPolluteGlobalScope()
    {
        _runtime.Modules.AddModule("isolated", @"
            (def secret-value 42)
        ");
        // Import should work
        var result = _runtime.Eval("(import isolated) isolated/secret-value");
        Assert.Equal(42.0, result.AsNumber());

        // But bare 'secret-value' should not exist in global scope
        Assert.Throws<JanetException>(() => _runtime.Eval("secret-value"));
    }

    // --- Import variants ---

    [Fact]
    public void Import_WithDefaultPrefix()
    {
        _runtime.Modules.AddModule("greet", @"
            (defn hello [] ""world"")
        ");
        var result = _runtime.Eval(@"
            (import greet)
            (greet/hello)
        ");
        Assert.Equal("world", result.AsString());
    }

    [Fact]
    public void Import_WithAsAlias()
    {
        _runtime.Modules.AddModule("longname", @"
            (def x 99)
        ");
        var result = _runtime.Eval(@"
            (import longname :as ln)
            ln/x
        ");
        Assert.Equal(99.0, result.AsNumber());
    }

    [Fact]
    public void Import_WithEmptyPrefix()
    {
        _runtime.Modules.AddModule("flat", @"
            (def value 77)
        ");
        var result = _runtime.Eval(@"
            (import flat :prefix """")
            value
        ");
        Assert.Equal(77.0, result.AsNumber());
    }

    // --- Caching ---

    [Fact]
    public void Import_TwiceReturnsSameResult()
    {
        _runtime.Modules.AddModule("cached", @"
            (def counter 1)
        ");
        var r1 = _runtime.Eval("(import cached) cached/counter");
        var r2 = _runtime.Eval("cached/counter");
        Assert.Equal(1.0, r1.AsNumber());
        Assert.Equal(1.0, r2.AsNumber());
    }

    // --- Module dependencies ---

    [Fact]
    public void AddModule_DependsOnAnotherModule()
    {
        _runtime.Modules.AddModule("base", @"
            (defn base-fn [x] (* x 10))
        ");
        _runtime.Modules.AddModule("derived", @"
            (import base)
            (defn derived-fn [x] (+ (base/base-fn x) 1))
        ");
        var result = _runtime.Eval(@"
            (import derived)
            (derived/derived-fn 5)
        ");
        Assert.Equal(51.0, result.AsNumber());
    }

    // --- Error handling ---

    [Fact]
    public void AddModule_InvalidSourceThrows()
    {
        Assert.Throws<JanetException>(() =>
            _runtime.Modules.AddModule("bad", "(this is not valid janet +++"));
    }

    [Fact]
    public void AddModule_InvalidNameThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            _runtime.Modules.AddModule("path/mod", "(def x 1)"));
        Assert.Throws<ArgumentException>(() =>
            _runtime.Modules.AddModule("file.janet", "(def x 1)"));
        Assert.Throws<ArgumentException>(() =>
            _runtime.Modules.AddModule("@scoped", "(def x 1)"));
        Assert.Throws<ArgumentException>(() =>
            _runtime.Modules.AddModule("", "(def x 1)"));
    }

    // --- Pre-built table ---

    [Fact]
    public void AddModule_FromJanetTable()
    {
        // Build a proper module environment table via Janet code
        _runtime.Eval(@"
            (def _prebuilt (make-env))
            (put _prebuilt 'answer @{:value 42})
        ");
        using var tbl = _runtime.Eval("_prebuilt").AsTable();
        _runtime.Modules.AddModule("prebuilt", tbl);

        var result = _runtime.Eval(@"
            (import prebuilt)
            prebuilt/answer
        ");
        Assert.Equal(42.0, result.AsNumber());
    }

    // --- RegisterLoader ---

    [Fact]
    public void RegisterLoader_IsRegisteredInModuleLoaders()
    {
        bool wasInvoked = false;
        _runtime.Modules.RegisterLoader("test-loader", (args) =>
        {
            wasInvoked = true;
            return Janet.Nil;
        });

        // Verify the loader is in module/loaders (C# callbacks are cfunctions)
        var result = _runtime.Eval("(type (get module/loaders :test-loader))");
        Assert.Equal("cfunction", result.AsString());
        Assert.False(wasInvoked); // Not invoked without a matching path entry
    }

    // --- C# callback integration ---

    [Fact]
    public void AddModule_WithCSharpCallback()
    {
        using var cb = _runtime.Register("_temp_amplify", (args) =>
            Janet.From(args[0].AsNumber() * 100));

        _runtime.Modules.AddModule("interop", @"
            (defn amplify [x] (_temp_amplify x))
        ");

        var result = _runtime.Eval(@"
            (import interop)
            (interop/amplify 5)
        ");
        Assert.Equal(500.0, result.AsNumber());
    }

    // --- Mixed definitions ---

    [Fact]
    public void AddModule_WithDefsAndMacros()
    {
        _runtime.Modules.AddModule("mixed", @"
            (def constant 100)
            (defn compute [x] (+ x constant))
        ");
        var result = _runtime.Eval(@"
            (import mixed)
            (mixed/compute 23)
        ");
        Assert.Equal(123.0, result.AsNumber());
    }

    // --- IsModuleCached ---

    [Fact]
    public void IsModuleCached_ReturnsTrueAfterAddModule()
    {
        Assert.False(_runtime.Modules.IsModuleCached("checkmod"));
        _runtime.Modules.AddModule("checkmod", "(def x 1)");
        Assert.True(_runtime.Modules.IsModuleCached("checkmod"));
    }
}
