using System;
using System.Diagnostics;
using System.Threading;

namespace NanoThrottle.Single
{
    // Uses token bucket algorithm
    // All timings are using Stopwatch ticks rather than DateTime ticks
    public class RateLimiter : IRateLimiter
    {
        private RateLimit _localRateLimit;
        private RateLimit _globalRateLimit;
        private int _instanceCount;
        private long _addTokenIntervalTicks;
        private int _maxTokens;
        private long _lastUpdatedTicks;
     
        private readonly Action _onSuccess;
        private readonly Action _onFailure;
        private readonly Action<RateLimitChangedNotification> _onRateLimitChanged;

        private volatile int _tokenCount;
        private volatile int _isLocked;

        private readonly object _updateSettingsLock = new Object();

        internal RateLimiter(
            string name,
            RateLimit rateLimit,
            int instanceCount = 1,
            Action onSuccess = null,
            Action onFailure = null,
            Action<RateLimitChangedNotification> onRateLimitChanged = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            InstanceCount = instanceCount;
            _localRateLimit = rateLimit.AsLocal(instanceCount);
            _globalRateLimit = rateLimit.AsGlobal(instanceCount);
            _addTokenIntervalTicks = GetIntervalBetweenEachTokenRefresh(_localRateLimit);
            _maxTokens = _localRateLimit.Count;
            _tokenCount = _localRateLimit.Count;
            _lastUpdatedTicks = Stopwatch.GetTimestamp();

            _onSuccess = onSuccess;
            _onFailure = onFailure;
            _onRateLimitChanged = onRateLimitChanged;
        }
        
        public string Name { get; }

        public RateLimit GetRateLimit(RateLimitType type = RateLimitType.Global)
        {
            return type == RateLimitType.Local
                ? _localRateLimit
                : _globalRateLimit;
        }

        public void SetRateLimit(RateLimit rateLimit)
        {
            lock (_updateSettingsLock)
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
        
        public int InstanceCount
        {
            get => _instanceCount;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(InstanceCount));
                
                lock (_updateSettingsLock)
                    _instanceCount = value;
            }
        }

        public bool CanExecute(int count = 1)
        {
            UpdateTokens();
            
            var success = TryTakeTokens(count);
            
            if (success)
                _onSuccess?.Invoke();
            else
                _onFailure?.Invoke();

            return success;
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
            if (Interlocked.CompareExchange(ref _isLocked, 1, 0) != 0)
                return;

            var tokensToAdd = (int)((now - lastUpdated) / _addTokenIntervalTicks);

            var newTokenCount = Interlocked.Add(ref _tokenCount, tokensToAdd);

            if (newTokenCount > _maxTokens)
                _tokenCount = _maxTokens;

            var lastUpdatedIncrement = tokensToAdd * _addTokenIntervalTicks;

            Interlocked.Add(ref _lastUpdatedTicks, lastUpdatedIncrement);

            _isLocked = 0;
        }

        private static long GetIntervalBetweenEachTokenRefresh(RateLimit rateLimit)
        {
            return (long)(Stopwatch.Frequency * rateLimit.Interval.TotalSeconds / rateLimit.Count);
        }
    }
}