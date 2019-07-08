using System;

namespace NanoThrottle
{
    public readonly struct RateLimit
    {
        public RateLimit(int count, TimeSpan interval, int maxBurst)
        {
            Count = count;
            Interval = interval;
            MaxBurst = maxBurst;
        }
        
        public int Count { get; }
        public TimeSpan Interval { get; }
        public int MaxBurst { get; }
    }
}