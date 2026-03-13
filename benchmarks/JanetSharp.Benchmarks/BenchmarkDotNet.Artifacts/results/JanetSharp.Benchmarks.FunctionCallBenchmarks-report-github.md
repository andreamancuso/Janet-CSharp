```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8037)
Intel Core i7-9750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2


```
| Method              | Mean       | Error    | StdDev   | Median     | Allocated |
|-------------------- |-----------:|---------:|---------:|-----------:|----------:|
| InvokeJanetFunction |   957.9 ns | 95.26 ns | 276.4 ns |   878.4 ns |         - |
| CallbackRoundTrip   | 3,955.4 ns | 78.39 ns | 128.8 ns | 3,921.2 ns |         - |
