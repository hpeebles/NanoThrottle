namespace NanoThrottle.Single
{
    public readonly struct RateLimitChangedNotification
    {
        public RateLimitChangedNotification(RateLimit oldRateLimit, RateLimit newRateLimit)
        {
            OldRateLimit = oldRateLimit;
            NewRateLimit = newRateLimit;
        }
        
        public RateLimit OldRateLimit { get; }
        public RateLimit NewRateLimit { get; }
    }
}