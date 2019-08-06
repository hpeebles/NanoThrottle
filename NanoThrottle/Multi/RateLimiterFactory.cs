using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace NanoThrottle.Multi
{
    public class RateLimiterFactory<TK>
    {
        private readonly IObservable<IEnumerable<KeyValuePair<TK, RateLimit>>> _rateLimitUpdates;
        private IObservable<int> _instanceCountUpdates;
        private IEqualityComparer<TK> _comparer;
        private Action<TK> _onSuccess;
        private Action<TK> _onFailure;
        private Action<RateLimitChangedNotification<TK>> _onRateLimitChanged;
        private Action<InstanceCountChangedNotification> _onInstanceCountChanged;
        private Action<RateLimiter<TK>> _onBuild;

        internal RateLimiterFactory(IObservable<IEnumerable<KeyValuePair<TK, RateLimit>>> rateLimitUpdates)
        {
            _rateLimitUpdates = rateLimitUpdates;
        }

        public RateLimiterFactory<TK> WithInstanceCount(int instanceCount)
        {
            if (instanceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(instanceCount));
            
            return WithInstanceCount(Observable.Return(instanceCount));
        }
        
        public RateLimiterFactory<TK> WithInstanceCount(IObservable<int> instanceCountUpdates)
        {
            _instanceCountUpdates = instanceCountUpdates ?? throw new ArgumentNullException(nameof(instanceCountUpdates));
            return this;
        }

        public RateLimiterFactory<TK> WithKeyComparer(IEqualityComparer<TK> comparer)
        {
            _comparer = comparer;
            return this;
        }

        public RateLimiterFactory<TK> OnSuccess(Action<TK> onSuccess)
        {
            _onSuccess += onSuccess;
            return this;
        }

        public RateLimiterFactory<TK> OnFailure(Action<TK> onFailure)
        {
            _onFailure += onFailure;
            return this;
        }

        public RateLimiterFactory<TK> OnRateLimitChanged(Action<RateLimitChangedNotification<TK>> onRateLimitChanged)
        {
            _onRateLimitChanged += onRateLimitChanged;
            return this;
        }
        
        public RateLimiterFactory<TK> OnInstanceCountChanged(Action<InstanceCountChangedNotification> onInstanceCountChanged)
        {
            _onInstanceCountChanged += onInstanceCountChanged;
            return this;
        }

        public RateLimiterFactory<TK> OnBuild(Action<RateLimiter<TK>> onBuild)
        {
            _onBuild += onBuild;
            return this;
        }
        
        public IRateLimiter<TK> Build()
        {
            var rateLimiter = new RateLimiter<TK>(
                _rateLimitUpdates,
                _instanceCountUpdates,
                _comparer,
                _onSuccess,
                _onFailure,
                _onRateLimitChanged,
                _onInstanceCountChanged);
            
            _onBuild?.Invoke(rateLimiter);

            return rateLimiter;
        }
    }
}