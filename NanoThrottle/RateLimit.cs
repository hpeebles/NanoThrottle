using System;

namespace NanoThrottle
{
    public readonly struct RateLimit : IEquatable<RateLimit>
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

        public bool Equals(RateLimit other)
        {
            return Count == other.Count && Interval.Equals(other.Interval);
        }

        public override bool Equals(object obj)
        {
            return obj is RateLimit other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Count * 397) ^ Interval.GetHashCode();
            }
        }

        public static bool operator ==(RateLimit left, RateLimit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RateLimit left, RateLimit right)
        {
            return !left.Equals(right);
        }
    }
}