using System;

namespace NanoThrottle.Single
{
    public class RateLimiterFactory
    {
        private readonly string _name;
        private RateLimit _rateLimit;
        private Action _onSuccess;
        private Action _onFailure;
        private Action<RateLimiter> _onBuild;

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

        public RateLimiterFactory OnSuccess(Action onSuccess)
        {
            _onSuccess += onSuccess;
            return this;
        }

        public RateLimiterFactory OnFailure(Action onFailure)
        {
            _onFailure += onFailure;
            return this;
        }

        public RateLimiterFactory OnBuild(Action<RateLimiter> onBuild)
        {
            _onBuild += onBuild;
            return this;
        }

        public IRateLimiter Build()
        {
            return new RateLimiter(_name, _rateLimit, _onSuccess, _onFailure);
        }
    }
}