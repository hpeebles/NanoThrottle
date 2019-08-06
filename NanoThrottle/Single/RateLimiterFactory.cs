using System;
using System.Reactive.Linq;

namespace NanoThrottle.Single
{
    public class RateLimiterFactory
    {
        private readonly IObservable<RateLimit> _rateLimitUpdates;
        private IObservable<int> _instanceCountUpdates;
        private Action _onSuccess;
        private Action _onFailure;
        private Action<RateLimitChangedNotification> _onRateLimitChanged;
        private Action<InstanceCountChangedNotification> _onInstanceCountChanged;
        private Action<RateLimiter> _onBuild;

        internal RateLimiterFactory(IObservable<RateLimit> rateLimitUpdates)
        {
            _rateLimitUpdates = rateLimitUpdates;
        }
        
        public RateLimiterFactory WithInstanceCount(int instanceCount)
        {
            if (instanceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(instanceCount));
            
            _instanceCountUpdates = Observable.Return(instanceCount);
            return this;
        }
        
        public RateLimiterFactory WithInstanceCount(IObservable<int> instanceCountUpdates)
        {
            _instanceCountUpdates = instanceCountUpdates ?? throw new ArgumentNullException(nameof(instanceCountUpdates));
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
                _rateLimitUpdates,
                _instanceCountUpdates,
                _onSuccess,
                _onFailure,
                _onRateLimitChanged,
                _onInstanceCountChanged);
            
            _onBuild?.Invoke(rateLimiter);

            return rateLimiter;
        }
    }
}