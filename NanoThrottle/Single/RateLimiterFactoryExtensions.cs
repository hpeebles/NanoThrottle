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
                rateLimitUpdates
                    .Do(rateLimiter.SetRateLimit)
                    .Retry()
                    .Subscribe();
            }
        }
    }
}