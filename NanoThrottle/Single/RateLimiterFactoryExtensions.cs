using System;
using System.Reactive.Linq;

namespace NanoThrottle.Single
{
    public static class RateLimiterFactoryExtensions
    {
        public static RateLimiterFactory WithRateLimitUpdates(
            this RateLimiterFactory factory,
            IObservable<RateLimit> rateLimitUpdates)
        {
            return factory.OnBuild(Subscribe);

            void Subscribe(IRateLimiter rateLimiter)
            {
                rateLimitUpdates.Subscribe(rateLimiter.SetRateLimit);
            }
        }
        
        public static RateLimiterFactory WithInstanceCountUpdates(
            this RateLimiterFactory factory,
            IObservable<int> instanceCountUpdates)
        {
            return factory.OnBuild(Subscribe);

            void Subscribe(IRateLimiter rateLimiter)
            {
                instanceCountUpdates.Subscribe(count => rateLimiter.InstanceCount = count);
            }
        }
    }
}