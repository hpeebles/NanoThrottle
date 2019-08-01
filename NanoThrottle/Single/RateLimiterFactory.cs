using System;

namespace NanoThrottle.Single
{
    public class RateLimiterFactory
    {
        private readonly RateLimit _rateLimit;
        private int _instanceCount = 1;
        private Action _onSuccess;
        private Action _onFailure;
        private Action<RateLimitChangedNotification> _onRateLimitChanged;
        private Action<InstanceCountChangedNotification> _onInstanceCountChanged;
        private Action<RateLimiter> _onBuild;

        internal RateLimiterFactory(RateLimit rateLimit)
        {
            _rateLimit = rateLimit;
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
        
        public RateLimiterFactory OnInstanceCountChanged(Action<InstanceCountChangedNotification> onInstanceCountChanged)
        {
            _onInstanceCountChanged += onInstanceCountChanged;
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
                _rateLimit,
                _instanceCount,
                _onSuccess,
                _onFailure,
                _onRateLimitChanged,
                _onInstanceCountChanged);
            
            _onBuild?.Invoke(rateLimiter);

            return rateLimiter;
        }
    }
}