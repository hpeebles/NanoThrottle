using System;
using System.Diagnostics;
using System.Threading;

namespace NanoThrottle.Single
{
    // Uses token bucket algorithm
    // All timings are using Stopwatch ticks rather than DateTime ticks
    public class RateLimiter : IRateLimiter
    {
        private RateLimit _rateLimit;
        private long _addTokenIntervalTicks;
        private int _maxTokens;
        private long _lastUpdatedTicks;
        
        private volatile int _tokenCount;
        private volatile int _isLocked;

        private readonly object _updateLock = new Object();

        internal RateLimiter(string name, RateLimit rateLimit)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            RateLimit = rateLimit;
            _tokenCount = rateLimit.Count;
            _lastUpdatedTicks = Stopwatch.GetTimestamp();
        }
        
        public string Name { get; }

        public RateLimit RateLimit
        {
            get => _rateLimit;
            set
            {
                lock (_updateLock)
                {
                    _rateLimit = value;
                    _addTokenIntervalTicks = GetIntervalBetweenEachTokenRefresh(value);
                    _maxTokens = value.Count;

                    if (_tokenCount > _maxTokens)
                        _tokenCount = _maxTokens;
                }
            }
        }
        
        public bool CanExecute(int count = 1)
        {
            UpdateTokens();
            
            return TryTakeTokens(count);
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