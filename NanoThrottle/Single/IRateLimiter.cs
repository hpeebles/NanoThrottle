namespace NanoThrottle.Single
{
    public interface IRateLimiter
    {
        string Name { get; }

        RateLimit GetRateLimit(RateLimitType type = RateLimitType.Global);

        void SetRateLimit(RateLimit rateLimit);
        
        int InstanceCount { get; set; }
        
        bool CanExecute(int count = 1);
    }
}