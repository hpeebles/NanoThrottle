namespace NanoThrottle.Single
{
    public readonly struct RateLimitChangedNotification
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
    }
}