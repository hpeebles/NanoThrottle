using System;

namespace NanoThrottle.Multi
{
    public interface IRateLimiter<in TK> : IDisposable
    {
        RateLimiterState State { get; }
        
        void WaitUntilInitialized(TimeSpan timeout);
        
        bool CanExecute(TK key, int count = 1);

        RateLimit GetRateLimit(TK key, RateLimitType type = RateLimitType.Global);
        
        void SetRateLimit(TK key, RateLimit rateLimit);
        
        int InstanceCount { get; set; }
    }
}