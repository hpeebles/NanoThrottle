using System;
using NanoThrottle.Multi;

namespace NanoThrottle.Redis
{
    public static class RedisRateLimiterFactoryExtensions
    {
        public static RateLimiterFactory<TK> MaintainInstanceCountViaRedis<TK>(
            this RateLimiterFactory<TK> factory,
            RedisConfiguration redisConfiguration,
            string key,
            int minInstanceCount = 1,
            int maxInstanceCount = Int32.MaxValue,
            TimeSpan? refreshInterval = null)
        {
            var instanceCountObservable = InstanceCountObservableFactory.Create(
                redisConfiguration,
                key,
                minInstanceCount,
                maxInstanceCount,
                refreshInterval ?? TimeSpan.FromSeconds(10));

            return factory.WithInstanceCount(instanceCountObservable);
        }
    }
}