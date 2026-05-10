// Benchmark entry point — run in Release mode only
// Usage: dotnet run -c Release --project tests/Mediatore.Benchmarks/Mediatore.Benchmarks.csproj
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
