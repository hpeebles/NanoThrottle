using System;
using System.Threading;

namespace NanoThrottle.Single
{
    public interface IRateLimiter : IDisposable
    {
        RateLimiterState State { get; }
        
        void WaitUntilInitialized(TimeSpan timeout);
        
        void WaitUntilInitialized(CancellationToken token);
        
        RateLimit GetRateLimit(RateLimitType type = RateLimitType.Global);

        void SetRateLimit(RateLimit rateLimit);
        
        int InstanceCount { get; set; }
        
        bool CanExecute(int count = 1);
    }
}