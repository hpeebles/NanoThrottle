namespace NanoThrottle.Multi
{
    public readonly struct RateLimitChangedNotification<TK>
    {
        public RateLimitChangedNotification(TK key, RateLimit oldRateLimit, RateLimit newRateLimit)
        {
            Key = key;
            OldRateLimit = oldRateLimit;
            NewRateLimit = newRateLimit;
        }

        public TK Key { get; }
        public RateLimit OldRateLimit { get; }
        public RateLimit NewRateLimit { get; }
    }
}