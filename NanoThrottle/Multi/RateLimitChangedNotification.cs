namespace NanoThrottle.Multi
{
    public readonly struct RateLimitChangedNotification<TK>
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
    }
}