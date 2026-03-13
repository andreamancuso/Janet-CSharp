```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8037)
Intel Core i7-9750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2


```
| Method           | Mean      | Error     | StdDev    | Allocated |
|----------------- |----------:|----------:|----------:|----------:|
| EvalArithmetic   |  3.202 μs | 0.0557 μs | 0.0521 μs |         - |
| EvalStringConcat |  4.845 μs | 0.0967 μs | 0.1588 μs |         - |
| EvalDefnAndCall  | 14.661 μs | 0.2884 μs | 0.4317 μs |         - |
| EvalMapFilter    | 67.949 μs | 1.3475 μs | 2.1372 μs |         - |
| EvalLoop         | 62.383 μs | 1.2232 μs | 1.3595 μs |         - |
