using System;

namespace NanoThrottle.Single
{
    public class RateLimitChangedNotification : IEquatable<RateLimitChangedNotification>
    {
        public RateLimitChangedNotification(
            RateLimit oldLocalRateLimit,
            RateLimit newLocalRateLimit,
            RateLimit oldGlobalRateLimit,
            RateLimit newGlobalRateLimit)
        {
            OldLocalRateLimit = oldLocalRateLimit;
            NewLocalRateLimit = newLocalRateLimit;
            OldGlobalRateLimit = oldGlobalRateLimit;
            NewGlobalRateLimit = newGlobalRateLimit;
        }
        
        public RateLimit OldLocalRateLimit { get; }
        public RateLimit NewLocalRateLimit { get; }
        public RateLimit OldGlobalRateLimit { get; }
        public RateLimit NewGlobalRateLimit { get; }

        public bool Equals(RateLimitChangedNotification other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return
                OldLocalRateLimit.Equals(other.OldLocalRateLimit) &&
                NewLocalRateLimit.Equals(other.NewLocalRateLimit) &&
                OldGlobalRateLimit.Equals(other.OldGlobalRateLimit) &&
                NewGlobalRateLimit.Equals(other.NewGlobalRateLimit);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(RateLimitChangedNotification)) return false;
            return Equals((RateLimitChangedNotification) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OldLocalRateLimit.GetHashCode();
                hashCode = (hashCode * 397) ^ NewLocalRateLimit.GetHashCode();
                hashCode = (hashCode * 397) ^ OldGlobalRateLimit.GetHashCode();
                hashCode = (hashCode * 397) ^ NewGlobalRateLimit.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(RateLimitChangedNotification left, RateLimitChangedNotification right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RateLimitChangedNotification left, RateLimitChangedNotification right)
        {
            return !Equals(left, right);
        }
    }
}