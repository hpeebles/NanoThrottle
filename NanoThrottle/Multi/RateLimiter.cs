using System;
using System.Collections.Generic;
using System.Linq;
using NanoThrottle.Single;

namespace NanoThrottle.Multi
{
    public class RateLimiter<TK> : IRateLimiter<TK>
    {
        private readonly IDictionary<TK, IRateLimiter> _rateLimiters;

        internal RateLimiter(
            string name,
            IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits,
            IEqualityComparer<TK> comparer = null,
            Action<TK> onSuccess = null,
            Action<TK> onFailure = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (rateLimits == null) throw new ArgumentNullException(nameof(rateLimits));
            
            _rateLimiters = rateLimits.ToDictionary(
                kv => kv.Key,
                kv => (IRateLimiter)new RateLimiter(
                    $"{name}_{kv.Key}",
                    kv.Value,
                    ConvertAction(onSuccess, kv.Key),
                    ConvertAction(onFailure, kv.Key)),
                comparer);
        }
        
        public string Name { get; }

        public bool CanExecute(TK key, int count = 1)
        {
            return GetRateLimiterSingle(key).CanExecute(count);
        }

        public RateLimit GetRateLimit(TK key)
        {
            return GetRateLimiterSingle(key).RateLimit;
        }

        public void SetRateLimit(TK key, RateLimit rateLimit)
        {
            GetRateLimiterSingle(key).RateLimit = rateLimit;
        }

        private IRateLimiter GetRateLimiterSingle(TK key)
        {
            return _rateLimiters.TryGetValue(key, out var rateLimiter)
                ? rateLimiter
                : throw new Exception($"No rate limit defined for key '{key}'");
        }

        private static Action ConvertAction(Action<TK> action, TK key)
        {
            if (action == null)
                return null;

            return () => action(key);
        }
    }
}