using System;

namespace NanoThrottle
{
    public readonly struct RateLimit
    {
        public RateLimit(int count, TimeSpan interval)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), $"'{nameof(count)}' must be >= 0");
            if (interval == TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval), $"'{nameof(interval)}' must be a non-zero {nameof(TimeSpan)}");
            
            Count = count;
            Interval = interval;
        }
        
        public int Count { get; }
        public TimeSpan Interval { get; }
    }
}