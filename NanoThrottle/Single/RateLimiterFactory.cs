using System;

namespace NanoThrottle.Single
{
    public class RateLimiterFactory
    {
        private readonly string _name;
        private RateLimit _rateLimit;
        private int _instanceCount = 1;
        private Action _onSuccess;
        private Action _onFailure;
        private Action<RateLimitChangedNotification> _onRateLimitChanged;
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
        
        public RateLimiterFactory WithInstanceCount(int instanceCount)
        {
            if (instanceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(instanceCount));
            
            _instanceCount = instanceCount;
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

        public RateLimiterFactory OnRateLimitChanged(Action<RateLimitChangedNotification> onRateLimitChanged)
        {
            _onRateLimitChanged += onRateLimitChanged;
            return this;
        }

        public RateLimiterFactory OnBuild(Action<RateLimiter> onBuild)
        {
            _onBuild += onBuild;
            return this;
        }

        public IRateLimiter Build()
        {
            var rateLimiter = new RateLimiter(
                _name,
                _rateLimit,
                _instanceCount,
                _onSuccess,
                _onFailure,
                _onRateLimitChanged);
            
            _onBuild?.Invoke(rateLimiter);

            return rateLimiter;
        }
    }
}