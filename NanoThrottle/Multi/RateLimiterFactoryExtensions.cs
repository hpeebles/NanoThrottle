using System;
using System.Collections.Generic;

namespace NanoThrottle.Multi
{
    public static class RateLimiterFactoryExtensions
    {
        public static RateLimiterFactory<TK> WithRateLimitUpdates<TK>(
            this RateLimiterFactory<TK> factory,
            IObservable<KeyValuePair<TK, RateLimit>> rateLimitUpdates)
        {
            return factory.OnBuild(Subscribe);

            void Subscribe(IRateLimiter<TK> rateLimiter)
            {
                rateLimitUpdates.Subscribe(kv => rateLimiter.SetRateLimit(kv.Key, kv.Value));
            }
        }
        
        public static RateLimiterFactory<TK> WithInstanceCountUpdates<TK>(
            this RateLimiterFactory<TK> factory,
            IObservable<int> instanceCountUpdates)
        {
            return factory.OnBuild(Subscribe);

            void Subscribe(IRateLimiter<TK> rateLimiter)
            {
                instanceCountUpdates.Subscribe(count => rateLimiter.InstanceCount = count);
            }
        }
    }
}