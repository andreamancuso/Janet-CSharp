# Calling C# from Janet

## Registering Callbacks

Use `JanetRuntime.Register` to expose a C# function to Janet code:

```csharp
using var runtime = new JanetRuntime();

// Register a named function
using var cb = runtime.Register("greet", args =>
{
    string name = args[0].AsString();
    return Janet.From($"Hello, {name}!");
});

// Call it from Janet
var result = runtime.Eval("(greet \"World\")");
Console.WriteLine(result.AsString()); // "Hello, World!"
```

## Callback Signature

The callback delegate is `JanetCallback.CallbackFunc`:

```csharp
public delegate Janet CallbackFunc(ReadOnlySpan<Janet> args);
```

- **Input**: `ReadOnlySpan<Janet>` — the arguments passed from Janet. Zero-copy, no allocation.
- **Output**: `Janet` — the return value. Return `Janet.Nil` for void-like functions.

## Argument Handling

```csharp
using var cb = runtime.Register("add-numbers", args =>
{
    // Check argument count
    if (args.Length != 2)
        throw new ArgumentException("Expected 2 arguments");

    // Unwrap typed values
    double a = args[0].AsNumber();
    double b = args[1].AsNumber();

    return Janet.From(a + b);
});
```

## Exception Safety

C# exceptions thrown inside callbacks are caught by the trampoline and converted to Janet errors:

```csharp
using var cb = runtime.Register("safe-div", args =>
{
    double divisor = args[1].AsNumber();
    if (divisor == 0)
        throw new DivideByZeroException("Cannot divide by zero");

    return Janet.From(args[0].AsNumber() / divisor);
});

// In Janet, this becomes an error signal
try
{
    runtime.Eval("(safe-div 10 0)");
}
catch (JanetException ex)
{
    // ex.ErrorValue contains the exception message
    Console.WriteLine(ex.ErrorValue.AsString()); // "Cannot divide by zero"
}
```

The C-side trampoline ensures that `longjmp` (used by Janet's `janet_panicv`) never unwinds through a managed frame.

## Lifetime Management

The `JanetCallback` object **must stay alive** as long as Janet code might call the function:

```csharp
// CORRECT: keep the callback alive
using var cb = runtime.Register("my-func", args => Janet.Nil);
runtime.Eval("(my-func)"); // works

// WRONG: callback disposed before use
var cb2 = runtime.Register("my-func-2", args => Janet.Nil);
cb2.Dispose();
runtime.Eval("(my-func-2)"); // undefined behavior!
```

## Advanced: Direct JanetCallback Construction

You can also create a `JanetCallback` without registering it in the environment:

```csharp
var cb = new JanetCallback(args => Janet.From(args[0].AsNumber() * 2));

// cb.Value is a Janet CFunction value you can pass to Janet code
// Remember to dispose when done
cb.Dispose();
```
