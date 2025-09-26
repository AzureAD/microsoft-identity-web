# Native AOT of Wilson

## How to run the benchmark

1. Run `publish.bat`. This builds the native AoT library (CrossPlatformValidation.dll) both for .NET 7 (leveraging
   Wilson 6) and .NET 8 (leveraging Wilson 7), and will copy these DLLs in the bin\Debug folder of CrossPlatformValidatorTests
   and the bin\Release folder of Benchmark for each of the target frameworks.
1. Run the benchmark for each framework:

   ```shell
   cd Benchmark
   dotnet run -f net8.0 -c Release
   ```

## Results

### Raw results

```text
BenchmarkDotNet v0.13.8, Windows 11 (10.0.22621.2283/22H2/2022Update/SunValley2) (Hyper-V)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100-rc.1.23423.3
  [Host] : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessNoEmitToolchain  IterationCount=15
LaunchCount=2  WarmupCount=10
```

| Method                       | Mean     | Error    | StdDev   | Allocated |
|----------------------------- |---------:|---------:|---------:|----------:|
| ValidateAuthRequestBenchmark (.NET 7, leveraging Wilson 6) | 92.63 us | 0.576 us | 0.844 us |     176 B |
| ValidateAuthRequestBenchmark (.NET 8, leveraging Wilson 7) | 42.59 us | 0.125 us | 0.175 us |     176 B |

### Summary:

| Config | Latency | RPS |
| -----  | --- | ----  |
| Wilson 6 AoT .NET 7 | 92.63 us | 10796
| Wilson 7 AoT .NET 8 | 42.59 us | 23479

Speed-up: x2.17 (that is 117% performance improvements)