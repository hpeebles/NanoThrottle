using System;

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
                rateLimitUpdates.Subscribe(r => rateLimiter.RateLimit = r);
            }
        }
    }
}