using System;
using System.Collections.Generic;
using System.Linq;

namespace NanoThrottle
{
    public class RateLimiter<TK> : IRateLimiter<TK>
    {
        private readonly IDictionary<TK, IRateLimiterSingle> _rateLimiters;

        internal RateLimiter(
            string name,
            IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits,
            IEqualityComparer<TK> comparer = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (rateLimits == null) throw new ArgumentNullException(nameof(rateLimits));
            
            _rateLimiters = rateLimits.ToDictionary(
                kv => kv.Key,
                kv => (IRateLimiterSingle)new RateLimiterSingle($"{name}_{kv.Key}", kv.Value),
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

        private IRateLimiterSingle GetRateLimiterSingle(TK key)
        {
            return _rateLimiters.TryGetValue(key, out var rateLimiter)
                ? rateLimiter
                : throw new Exception($"No rate limit defined for key '{key}'");
        }
    }
}