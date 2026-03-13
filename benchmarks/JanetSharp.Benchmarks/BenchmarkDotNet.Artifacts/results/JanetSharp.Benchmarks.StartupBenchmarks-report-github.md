```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8037)
Intel Core i7-9750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2
  Job-PFADGP : .NET 9.0.13 (9.0.1326.6317), X64 RyuJIT AVX2

InvocationCount=1  UnrollFactor=1  

```
| Method         | Mean     | Error     | StdDev    | Allocated |
|--------------- |---------:|----------:|----------:|----------:|
| RuntimeStartup | 1.736 ms | 0.0344 ms | 0.0870 ms |     440 B |
