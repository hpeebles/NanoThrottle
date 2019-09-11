using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;

namespace NanoThrottle.Single
{
    // Uses token bucket algorithm
    // All timings are using Stopwatch ticks rather than DateTime ticks
    public class RateLimiter : IRateLimiter
    {
        public static RateLimiterFactory WithRateLimit(RateLimit rateLimit)
        {
            return new RateLimiterFactory(Observable.Return(rateLimit));
        }
        
        public static RateLimiterFactory WithRateLimit(IObservable<RateLimit> rateLimitUpdates)
        {
            return new RateLimiterFactory(rateLimitUpdates);
        }
        
        private readonly IDisposable _rateLimitUpdatesSubscription;
        private readonly IDisposable _instanceCountUpdatesSubscription;
        private readonly Action _onSuccess;
        private readonly Action _onFailure;
        private readonly Action<RateLimitChangedNotification> _onRateLimitChanged;
        private readonly Action<InstanceCountChangedNotification> _onInstanceCountChanged;
        private readonly ManualResetEvent _initializationWaitHandle = new ManualResetEvent(false);
        private readonly object _updateSettingsLock = new Object();
        private RateLimit? _rateLimitPreInitialization;
        private RateLimit _localRateLimit;
        private RateLimit _globalRateLimit;
        private int _instanceCount;
        private long _addTokenIntervalTicks;
        private int _maxTokens;
        private long _lastUpdatedTicks;
        
        private volatile int _tokenCount;
        private volatile int _updateTokenCountLock;
        private RateLimiterState _state;

        internal RateLimiter(
            RateLimit rateLimit,
            int instanceCount = 1,
            Action onSuccess = null,
            Action onFailure = null,
            Action<RateLimitChangedNotification> onRateLimitChanged = null,
            Action<InstanceCountChangedNotification> onInstanceCountChanged = null)
            : this(
                Observable.Return(rateLimit),
                Observable.Return(instanceCount),
                onSuccess,
                onFailure,
                onRateLimitChanged,
                onInstanceCountChanged)
        { }
        
        internal RateLimiter(
            IObservable<RateLimit> rateLimitUpdates,
            IObservable<int> instanceCountUpdates = null,
            Action onSuccess = null,
            Action onFailure = null,
            Action<RateLimitChangedNotification> onRateLimitChanged = null,
            Action<InstanceCountChangedNotification> onInstanceCountChanged = null)
        {
            if (rateLimitUpdates == null)
                throw new ArgumentNullException(nameof(rateLimitUpdates));
            
            if (instanceCountUpdates == null)
                instanceCountUpdates = Observable.Return(1);
            
            _onSuccess = onSuccess;
            _onFailure = onFailure;
            _onRateLimitChanged = onRateLimitChanged;
            _onInstanceCountChanged = onInstanceCountChanged;
            _state = RateLimiterState.PendingInitialization;
            
            _rateLimitUpdatesSubscription = rateLimitUpdates.Subscribe(UpdateRateLimit);
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
        
        public RateLimit GetRateLimit(RateLimitType type = RateLimitType.Global)
        {
            return type == RateLimitType.Local
                ? _localRateLimit
                : _globalRateLimit;
        }

        public void SetRateLimit(RateLimit rateLimit)
        {
            UpdateRateLimit(rateLimit);
        }
        
        public int InstanceCount
        {
            get { CheckState(); return _instanceCount; }
            set => UpdateInstanceCount(value);
        }

        public bool CanExecute(int count = 1)
        {
            CheckState();
            
            UpdateTokens();
            
            var success = TryTakeTokens(count);
            
            if (success)
                _onSuccess?.Invoke();
            else
                _onFailure?.Invoke();

            return success;
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

        private bool TryTakeTokens(int count)
        {
            var tokensRemainingAfterAction = Interlocked.Add(ref _tokenCount, -count);
            
            if (tokensRemainingAfterAction >= 0)
                return true;

            // If we are not able to proceed, add the tokens back into the bucket
            Interlocked.Add(ref _tokenCount, count);
            return false;
        }

        private void UpdateTokens()
        {
            var now = Stopwatch.GetTimestamp();
            var lastUpdated = Volatile.Read(ref _lastUpdatedTicks);

            if (lastUpdated > now - _addTokenIntervalTicks)
                return;

            // Mutex to ensure tokens are only updated by a single thread
            if (Interlocked.CompareExchange(ref _updateTokenCountLock, 1, 0) != 0)
                return;

            var tokensToAdd = (int)((now - lastUpdated) / _addTokenIntervalTicks);

            var newTokenCount = Interlocked.Add(ref _tokenCount, tokensToAdd);

            if (newTokenCount > _maxTokens)
                _tokenCount = _maxTokens;

            var lastUpdatedIncrement = tokensToAdd * _addTokenIntervalTicks;

            Interlocked.Add(ref _lastUpdatedTicks, lastUpdatedIncrement);

            _updateTokenCountLock = 0;
        }

        private void UpdateRateLimit(RateLimit rateLimit)
        {
            lock (_updateSettingsLock)
                UpdateRateLimitWithinLock(rateLimit);
        }

        private void UpdateRateLimitWithinLock(RateLimit rateLimit)
        {
            if (_state == RateLimiterState.PendingInitialization)
            {
                _rateLimitPreInitialization = rateLimit;

                if (_instanceCount > 0)
                    InitializeRateLimiterWithinLock();
            }
            else if (_state == RateLimiterState.Ready)
            {
                var newLocalRateLimit = rateLimit.AsLocal(InstanceCount);
                var newGlobalRateLimit = rateLimit.AsGlobal(InstanceCount);

                if (_localRateLimit == newLocalRateLimit && _globalRateLimit == newGlobalRateLimit)
                    return;

                _addTokenIntervalTicks = GetIntervalBetweenEachTokenRefresh(newLocalRateLimit);
                _maxTokens = newLocalRateLimit.Count;

                if (_tokenCount > _maxTokens)
                    _tokenCount = _maxTokens;

                var oldLocalRateLimit = _localRateLimit;
                var oldGlobalRateLimit = _globalRateLimit;

                _localRateLimit = newLocalRateLimit;
                _globalRateLimit = newGlobalRateLimit;

                if (_onRateLimitChanged != null)
                {
                    var notification = new RateLimitChangedNotification(
                        oldLocalRateLimit,
                        newLocalRateLimit,
                        oldGlobalRateLimit,
                        newGlobalRateLimit);

                    _onRateLimitChanged(notification);
                }
            }
        }

        private void UpdateInstanceCount(int instanceCount)
        {
            if (instanceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(InstanceCount));
                
            lock (_updateSettingsLock)
            {
                if (instanceCount == _instanceCount)
                    return;
                
                switch (_state)
                {
                    case RateLimiterState.PendingInitialization:
                    {
                        _instanceCount = instanceCount;

                        if (_rateLimitPreInitialization.HasValue)
                            InitializeRateLimiterWithinLock();
                   
                        break;
                    }

                    case RateLimiterState.Ready:
                        var oldInstanceCount = _instanceCount;
                        _instanceCount = instanceCount;
                        
                        // Update the rate limit so that it picks up the new instance count
                        UpdateRateLimitWithinLock(_globalRateLimit);

                        _onInstanceCountChanged?.Invoke(new InstanceCountChangedNotification(oldInstanceCount, _instanceCount));
                        break;
                   
                    case RateLimiterState.Disposed:
                        throw new ObjectDisposedException(nameof(RateLimiter));
                }
            }
        }

        private void InitializeRateLimiterWithinLock()
        {
            var rateLimit = _rateLimitPreInitialization.Value;

            _globalRateLimit = rateLimit.AsGlobal(_instanceCount);
            _localRateLimit = rateLimit.AsLocal(_instanceCount);
            _addTokenIntervalTicks = GetIntervalBetweenEachTokenRefresh(_localRateLimit);
            _maxTokens = _localRateLimit.Count;
            _tokenCount = _localRateLimit.Count;
            _lastUpdatedTicks = Stopwatch.GetTimestamp();

            _state = RateLimiterState.Ready;
            _rateLimitPreInitialization = null;
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

        private static long GetIntervalBetweenEachTokenRefresh(RateLimit rateLimit)
        {
            var ticks = (long)(Stopwatch.Frequency * rateLimit.Interval.TotalSeconds / rateLimit.Count);

            return ticks > 0
                ? ticks
                : 1;
        }
    }
}