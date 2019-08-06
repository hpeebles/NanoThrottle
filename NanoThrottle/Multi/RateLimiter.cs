using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using NanoThrottle.Single;

namespace NanoThrottle.Multi
{
    public static class RateLimiter
    {
        public static RateLimiterFactory<TK> WithRateLimits<TK>(IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits)
        {
            return WithRateLimits(Observable.Return(rateLimits));
        }

        public static RateLimiterFactory<TK> WithRateLimits<TK>(
            IObservable<IEnumerable<KeyValuePair<TK, RateLimit>>> rateLimitUpdates)
        {
            return new RateLimiterFactory<TK>(rateLimitUpdates);
        }
    }
    
    public class RateLimiter<TK> : IRateLimiter<TK>
    {
        private readonly IDisposable _rateLimitUpdatesSubscription;
        private readonly IDisposable _instanceCountUpdatesSubscription;
        private readonly IEqualityComparer<TK> _comparer;
        private readonly Action<TK> _onSuccess;
        private readonly Action<TK> _onFailure;
        private readonly Action<RateLimitChangedNotification<TK>> _onRateLimitChanged;
        private readonly Action<InstanceCountChangedNotification> _onInstanceCountChanged;
        private readonly ManualResetEvent _initializationWaitHandle = new ManualResetEvent(false);
        private readonly object _updateSettingsLock = new Object();
        private IDictionary<TK, RateLimit> _rateLimitsPreInitialization;
        private IDictionary<TK, IRateLimiter> _rateLimiters;
        private int _instanceCount;
        private RateLimiterState _state;

        internal RateLimiter(
            IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits,
            int instanceCount = 1,
            IEqualityComparer<TK> comparer = null,
            Action<TK> onSuccess = null,
            Action<TK> onFailure = null,
            Action<RateLimitChangedNotification<TK>> onRateLimitChanged = null,
            Action<InstanceCountChangedNotification> onInstanceCountChanged = null)
            : this(
                Observable.Return(rateLimits),
                Observable.Return(instanceCount),
                comparer,
                onSuccess,
                onFailure,
                onRateLimitChanged,
                onInstanceCountChanged
            )
        { }
        
        internal RateLimiter(
            IObservable<IEnumerable<KeyValuePair<TK, RateLimit>>> rateLimitUpdates,
            IObservable<int> instanceCountUpdates = null,
            IEqualityComparer<TK> comparer = null,
            Action<TK> onSuccess = null,
            Action<TK> onFailure = null,
            Action<RateLimitChangedNotification<TK>> onRateLimitChanged = null,
            Action<InstanceCountChangedNotification> onInstanceCountChanged = null)
        {
            if (rateLimitUpdates == null)
                throw new ArgumentNullException(nameof(rateLimitUpdates));
            
            if (instanceCountUpdates == null)
                instanceCountUpdates = Observable.Return(1);
            
            _comparer = comparer;
            _onSuccess = onSuccess;
            _onFailure = onFailure;
            _onRateLimitChanged = onRateLimitChanged;
            _onInstanceCountChanged = onInstanceCountChanged;
            _state = RateLimiterState.PendingInitialization;

            _rateLimitUpdatesSubscription = rateLimitUpdates.Subscribe(UpdateRateLimits);
            _instanceCountUpdatesSubscription = instanceCountUpdates.Where(c => c > 0).Subscribe(UpdateInstanceCount);
        }

        public RateLimiterState State => _state;

        public void WaitUntilInitialized(TimeSpan timeout)
        {
            if (_state == RateLimiterState.Disposed)
                throw new ObjectDisposedException(nameof(RateLimiter));
            
            if (!_initializationWaitHandle.WaitOne(timeout))
                throw new Exception($"Initialization failed to complete in {timeout.TotalSeconds:#.##} seconds");
        }
        
        public bool CanExecute(TK key, int count = 1)
        {
            CheckState();
            
            return GetRateLimiterSingle(key).CanExecute(count);
        }

        public RateLimit GetRateLimit(TK key, RateLimitType type = RateLimitType.Global)
        {
            CheckState();
            
            return GetRateLimiterSingle(key).GetRateLimit(type);
        }

        public void SetRateLimit(TK key, RateLimit rateLimit)
        {
            UpdateRateLimits(new[] { new KeyValuePair<TK, RateLimit>(key, rateLimit) });
        }

        public int InstanceCount
        {
            get { CheckState(); return _instanceCount; }
            set => UpdateInstanceCount(value);
        }

        public void Dispose()
        {
            lock (_updateSettingsLock)
            {
                _state = RateLimiterState.Disposed;
                _rateLimitUpdatesSubscription.Dispose();
                _instanceCountUpdatesSubscription.Dispose();
            }
        }

        private IRateLimiter GetRateLimiterSingle(TK key)
        {
            return _rateLimiters.TryGetValue(key, out var rateLimiter)
                ? rateLimiter
                : throw new Exception($"No rate limit defined for key '{key}'");
        }

        private void UpdateRateLimits(IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits)
        {
            lock (_updateSettingsLock)
            {
                switch (_state)
                {
                    case RateLimiterState.Ready:
                    {
                        foreach (var kv in rateLimits)
                        {
                            var rateLimiter = GetRateLimiterSingle(kv.Key);
                        
                            rateLimiter.SetRateLimit(kv.Value);
                        }

                        break;
                    }

                    case RateLimiterState.PendingInitialization:
                    {
                        if (_rateLimitsPreInitialization == null)
                            _rateLimitsPreInitialization = new Dictionary<TK, RateLimit>();

                        foreach (var rateLimit in rateLimits)
                            _rateLimitsPreInitialization[rateLimit.Key] = rateLimit.Value;

                        if (_instanceCount > 0)
                            InitializeRateLimitersWithinLock();
                        
                        break;
                    }

                    default:
                        throw new ObjectDisposedException(nameof(RateLimiter));
                }
            }
        }

        private void UpdateInstanceCount(int instanceCount)
        {
            if (instanceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(instanceCount));
            
            lock (_updateSettingsLock)
            {
                if (instanceCount == _instanceCount)
                    return;
                
                switch (_state)
                {
                    case RateLimiterState.Ready:
                    {
                        foreach (var kv in _rateLimiters)
                            kv.Value.InstanceCount = instanceCount;

                        var oldInstanceCount = _instanceCount;
                        _instanceCount = instanceCount;

                        _onInstanceCountChanged?.Invoke(new InstanceCountChangedNotification(oldInstanceCount, instanceCount));
                        
                        break;
                    }

                    case RateLimiterState.PendingInitialization:
                    {
                        _instanceCount = instanceCount;
                    
                        if (_rateLimitsPreInitialization != null)
                            InitializeRateLimitersWithinLock();
                        
                        break;
                    }

                    default:
                        throw new ObjectDisposedException(nameof(RateLimiter));
                }
            }
        }
        
        private void InitializeRateLimitersWithinLock()
        {
            _rateLimiters = _rateLimitsPreInitialization.ToDictionary(
                kv => kv.Key,
                kv => (IRateLimiter) new Single.RateLimiter(
                    kv.Value,
                    _instanceCount,
                    ConvertAction(_onSuccess, kv.Key),
                    ConvertAction(_onFailure, kv.Key),
                    ConvertAction(_onRateLimitChanged, kv.Key)),
                _comparer);

            _state = RateLimiterState.Ready;
            _rateLimitsPreInitialization = null;
            _initializationWaitHandle.Set();
        }

        private void CheckState()
        {
            switch (_state)
            {
                case RateLimiterState.Ready:
                    return;
                case RateLimiterState.PendingInitialization:
                    throw new InvalidOperationException($"{nameof(RateLimiter)} has not been initialized");
                case RateLimiterState.Disposed:
                    throw new ObjectDisposedException(nameof(RateLimiter));
            }
        }

        private static Action ConvertAction(Action<TK> action, TK key)
        {
            if (action == null)
                return null;

            return () => action(key);
        }
        
        private static Action<RateLimitChangedNotification> ConvertAction(
            Action<RateLimitChangedNotification<TK>> action, TK key)
        {
            if (action == null)
                return null;

            return r =>
            {
                var notification = new RateLimitChangedNotification<TK>(
                    key,
                    r.OldLocalRateLimit,
                    r.NewLocalRateLimit,
                    r.OldGlobalRateLimit,
                    r.NewGlobalRateLimit);

                action(notification);
            };
        }
    }
}