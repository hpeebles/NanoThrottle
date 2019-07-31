using System;
using System.Collections.Generic;

namespace NanoThrottle.Multi
{
    public class RateLimitChangedNotification<TK> : IEquatable<RateLimitChangedNotification<TK>>
    {
        public RateLimitChangedNotification(
            TK key,
            RateLimit oldLocalRateLimit,
            RateLimit newLocalRateLimit,
            RateLimit oldGlobalRateLimit,
            RateLimit newGlobalRateLimit)
        {
            Key = key;
            OldLocalRateLimit = oldLocalRateLimit;
            NewLocalRateLimit = newLocalRateLimit;
            OldGlobalRateLimit = oldGlobalRateLimit;
            NewGlobalRateLimit = newGlobalRateLimit;
        }

        public TK Key { get; }
        public RateLimit OldLocalRateLimit { get; }
        public RateLimit NewLocalRateLimit { get; }
        public RateLimit OldGlobalRateLimit { get; }
        public RateLimit NewGlobalRateLimit { get; }

        public bool Equals(RateLimitChangedNotification<TK> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return
                EqualityComparer<TK>.Default.Equals(Key, other.Key) &&
                OldLocalRateLimit.Equals(other.OldLocalRateLimit) &&
                NewLocalRateLimit.Equals(other.NewLocalRateLimit) &&
                OldGlobalRateLimit.Equals(other.OldGlobalRateLimit) &&
                NewGlobalRateLimit.Equals(other.NewGlobalRateLimit);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(RateLimitChangedNotification<TK>)) return false;
            return Equals((RateLimitChangedNotification<TK>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<TK>.Default.GetHashCode(Key);
                hashCode = (hashCode * 397) ^ OldLocalRateLimit.GetHashCode();
                hashCode = (hashCode * 397) ^ NewLocalRateLimit.GetHashCode();
                hashCode = (hashCode * 397) ^ OldGlobalRateLimit.GetHashCode();
                hashCode = (hashCode * 397) ^ NewGlobalRateLimit.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(RateLimitChangedNotification<TK> left, RateLimitChangedNotification<TK> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RateLimitChangedNotification<TK> left, RateLimitChangedNotification<TK> right)
        {
            return !Equals(left, right);
        }
    }
}