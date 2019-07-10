using System.Collections.Generic;

namespace NanoThrottle.Multi
{
    public class RateLimiterFactory
    {
        private readonly string _name;

        private RateLimiterFactory(string name)
        {
            _name = name;
        }

        public static RateLimiterFactory Create(string name)
        {
            return new RateLimiterFactory(name);
        }

        public RateLimiterFactory<TK> WithRateLimits<TK>(IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits)
        {
            return new RateLimiterFactory<TK>(_name, rateLimits);
        }
    }

    public class RateLimiterFactory<TK>
    {
        private readonly string _name;
        private readonly IEnumerable<KeyValuePair<TK, RateLimit>> _rateLimits;
        private IEqualityComparer<TK> _comparer;

        internal RateLimiterFactory(
            string name,
            IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits)
        {
            _name = name;
            _rateLimits = rateLimits;
        }

        public RateLimiterFactory<TK> WithKeyComparer(IEqualityComparer<TK> comparer)
        {
            _comparer = comparer;
            return this;
        }
        
        public IRateLimiter<TK> Build()
        {
            return new RateLimiter<TK>(_name, _rateLimits, _comparer);
        }
    }
}