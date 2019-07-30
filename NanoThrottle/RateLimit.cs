using System;

namespace NanoThrottle
{
    public readonly struct RateLimit : IEquatable<RateLimit>
    {
        public RateLimit(int count, TimeSpan interval, RateLimitType type = RateLimitType.Global)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), $"'{nameof(count)}' must be >= 0");
            if (interval == TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval), $"'{nameof(interval)}' must be a non-zero {nameof(TimeSpan)}");
            
            Count = count;
            Interval = interval;
            Type = type;
        }

        public int Count { get; }
        public TimeSpan Interval { get; } 
        public RateLimitType Type { get; }

        public bool Equals(RateLimit other)
        {
            return Count == other.Count && Interval.Equals(other.Interval) && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is RateLimit other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Count;
                hashCode = (hashCode * 397) ^ Interval.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Type;
                return hashCode;
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

        internal RateLimit AsLocal(int instanceCount)
        {
            return Type == RateLimitType.Local
                ? this
                : new RateLimit(Count / instanceCount, Interval, RateLimitType.Local);
        }
        
        internal RateLimit AsGlobal(int instanceCount)
        {
            return Type == RateLimitType.Global
                ? this
                : new RateLimit(Count * instanceCount, Interval, RateLimitType.Global);
        }
    }
}