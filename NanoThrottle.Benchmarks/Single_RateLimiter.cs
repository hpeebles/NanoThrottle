using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using NanoThrottle.Single;

namespace NanoThrottle.Benchmarks
{
    public class Single_RateLimiter
    {
        private readonly IRateLimiter _rateLimiter1; // CanExecute will always return true and tokens will be updated on each iteration
        private readonly IRateLimiter _rateLimiter2; // CanExecute will always return true and tokens will not be updated on any iteration
        private readonly IRateLimiter _rateLimiter3; // CanExecute will always return false and tokens will not be updated on any iteration

        public Single_RateLimiter()
        {
            _rateLimiter1 = RateLimiter
                .WithRateLimit(new RateLimit(Int32.MaxValue, TimeSpan.FromMinutes(1)))
                .Build();

            _rateLimiter2 = RateLimiter
                .WithRateLimit(new RateLimit(Int32.MaxValue, TimeSpan.MaxValue))
                .Build();

            _rateLimiter3 = RateLimiter
                .WithRateLimit(new RateLimit(0, TimeSpan.FromMinutes(1)))
                .Build();
        }

        public static void Run()
        {
#if DEBUG
            var runner = new Single_RateLimiter();
            runner.UpdateTokens_ReturnTrue();
            runner.DontUpdateTokens_ReturnTrue();
            runner.DontUpdateTokens_ReturnFalse();
#else
            BenchmarkRunner.Run<Single_RateLimiter>(ManualConfig
                .Create(DefaultConfig.Instance)
                .With(MemoryDiagnoser.Default));
#endif
        }

        [Benchmark]
        public void UpdateTokens_ReturnTrue()
        {
            _rateLimiter1.CanExecute();
        }

        [Benchmark]
        public void DontUpdateTokens_ReturnTrue()
        {
            _rateLimiter2.CanExecute();
        }
        
        [Benchmark]
        public void DontUpdateTokens_ReturnFalse()
        {
            _rateLimiter3.CanExecute();
        }
    }
}

/*

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.17134.345 (1803/April2018Update/Redstone4)
Intel Core i7-4790 CPU 3.60GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
Frequency=3507518 Hz, Resolution=285.1019 ns, Timer=TSC
.NET Core SDK=2.2.105
  [Host]     : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT


|                       Method |     Mean |     Error |    StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |---------:|----------:|----------:|------:|------:|------:|----------:|
|      UpdateTokens_ReturnTrue | 35.75 ns | 0.2147 ns | 0.1904 ns |     - |     - |     - |         - |
|  DontUpdateTokens_ReturnTrue | 27.53 ns | 0.0548 ns | 0.0458 ns |     - |     - |     - |         - |
| DontUpdateTokens_ReturnFalse | 38.32 ns | 0.2113 ns | 0.1873 ns |     - |     - |     - |         - |

*/