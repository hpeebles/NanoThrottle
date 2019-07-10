namespace NanoThrottle
{
    public class RateLimiterSingleFactory
    {
        private readonly string _name;
        private RateLimit _rateLimit;

        private RateLimiterSingleFactory(string name)
        {
            _name = name;
        }
        
        public static RateLimiterSingleFactory Create(string name)
        {
            return new RateLimiterSingleFactory(name);
        }

        public RateLimiterSingleFactory WithRateLimit(RateLimit rateLimit)
        {
            _rateLimit = rateLimit;
            return this;
        }

        public IRateLimiterSingle Build()
        {
            return new RateLimiterSingle(_name, _rateLimit);
        }
    }
}