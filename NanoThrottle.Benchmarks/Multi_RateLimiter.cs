using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using NanoThrottle.Multi;

namespace NanoThrottle.Benchmarks
{
    public class Multi_RateLimiter
    {
        private readonly IRateLimiter<int> _rateLimiter1; // CanExecute will always return true and tokens will be updated on each iteration
        private readonly IRateLimiter<int> _rateLimiter2; // CanExecute will always return true and tokens will not be updated on any iteration
        private readonly IRateLimiter<int> _rateLimiter3; // CanExecute will always return false and tokens will not be updated on any iteration

        public Multi_RateLimiter()
        {
            _rateLimiter1 = RateLimiter
                .WithRateLimits(new[] { new KeyValuePair<int, RateLimit>(1, new RateLimit(Int32.MaxValue, TimeSpan.FromMinutes(1))) })
                .Build();

            _rateLimiter2 = RateLimiter
                .WithRateLimits(new[] { new KeyValuePair<int, RateLimit>(1, new RateLimit(Int32.MaxValue, TimeSpan.MaxValue)) })
                .Build();

            _rateLimiter3 = RateLimiter
                .WithRateLimits(new[] { new KeyValuePair<int, RateLimit>(1, new RateLimit(0, TimeSpan.FromMinutes(1))) })
                .Build();
        }

        public static void Run()
        {
#if DEBUG
            var runner = new Multi_RateLimiter();
            runner.UpdateTokens_ReturnTrue();
            runner.DontUpdateTokens_ReturnTrue();
            runner.DontUpdateTokens_ReturnFalse();
#else
            BenchmarkRunner.Run<Multi_RateLimiter>(ManualConfig
                .Create(DefaultConfig.Instance)
                .With(MemoryDiagnoser.Default));
#endif
        }

        [Benchmark]
        public void UpdateTokens_ReturnTrue()
        {
            _rateLimiter1.CanExecute(1);
        }

        [Benchmark]
        public void DontUpdateTokens_ReturnTrue()
        {
            _rateLimiter2.CanExecute(1);
        }
        
        [Benchmark]
        public void DontUpdateTokens_ReturnFalse()
        {
            _rateLimiter3.CanExecute(1);
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
|      UpdateTokens_ReturnTrue | 52.37 ns | 0.3862 ns | 0.3424 ns |     - |     - |     - |         - |
|  DontUpdateTokens_ReturnTrue | 44.08 ns | 0.2901 ns | 0.2714 ns |     - |     - |     - |         - |
| DontUpdateTokens_ReturnFalse | 52.94 ns | 0.3033 ns | 0.2837 ns |     - |     - |     - |         - |

*/