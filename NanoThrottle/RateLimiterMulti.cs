using System;
using System.Collections.Generic;
using System.Linq;

namespace NanoThrottle
{
    public class RateLimiterMulti<TK>
    {
        private readonly IDictionary<TK, IRateLimiter> _rateLimiters;

        public RateLimiterMulti(
            IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits,
            IEqualityComparer<TK> comparer = null)
        {
            if (rateLimits == null) throw new ArgumentNullException(nameof(rateLimits));
            
            _rateLimiters = rateLimits.ToDictionary(
                kv => kv.Key,
                kv => (IRateLimiter)new TokenBucketRateLimiter(kv.Value),
                comparer);
        }

        public bool CanExecute(TK key, int count = 1)
        {
            if (!_rateLimiters.TryGetValue(key, out var rateLimiter))
                throw new Exception($"No rate limit defined for key '{key}'");

            return rateLimiter.CanExecute(count);
        }
    }
}