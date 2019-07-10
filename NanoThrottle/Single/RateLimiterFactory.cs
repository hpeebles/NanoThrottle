namespace NanoThrottle.Single
{
    public class RateLimiterFactory
    {
        private readonly string _name;
        private RateLimit _rateLimit;

        private RateLimiterFactory(string name)
        {
            _name = name;
        }
        
        public static RateLimiterFactory Create(string name)
        {
            return new RateLimiterFactory(name);
        }

        public RateLimiterFactory WithRateLimit(RateLimit rateLimit)
        {
            _rateLimit = rateLimit;
            return this;
        }

        public IRateLimiter Build()
        {
            return new RateLimiter(_name, _rateLimit);
        }
    }
}