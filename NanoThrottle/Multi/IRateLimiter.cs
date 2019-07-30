namespace NanoThrottle.Multi
{
    public interface IRateLimiter<in TK>
    {
        string Name { get; }
        
        bool CanExecute(TK key, int count = 1);

        RateLimit GetRateLimit(TK key, RateLimitType type = RateLimitType.Global);
        
        void SetRateLimit(TK key, RateLimit rateLimit);
        
        int InstanceCount { get; set; }
    }
}