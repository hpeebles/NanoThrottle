using System;
using System.Collections.Generic;
using System.Linq;
using NanoThrottle.Single;

namespace NanoThrottle.Multi
{
    public static class RateLimiter
    {
        public static RateLimiterFactory<TK> WithRateLimits<TK>(IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits)
        {
            return new RateLimiterFactory<TK>(rateLimits);
        }
    }
    
    public class RateLimiter<TK> : IRateLimiter<TK>
    {
        private readonly IDictionary<TK, IRateLimiter> _rateLimiters;
        private readonly Action<InstanceCountChangedNotification> _onInstanceCountChanged;
        private readonly object _updateSettingsLock = new Object();
        private int _instanceCount;

        internal RateLimiter(
            IEnumerable<KeyValuePair<TK, RateLimit>> rateLimits,
            int instanceCount = 1,
            IEqualityComparer<TK> comparer = null,
            Action<TK> onSuccess = null,
            Action<TK> onFailure = null,
            Action<RateLimitChangedNotification<TK>> onRateLimitChanged = null,
            Action<InstanceCountChangedNotification> onInstanceCountChanged = null)
        {
            if (rateLimits == null) throw new ArgumentNullException(nameof(rateLimits));
            
            _rateLimiters = rateLimits.ToDictionary(
                kv => kv.Key,
                kv => (IRateLimiter)new Single.RateLimiter(
                    kv.Value,
                    instanceCount,
                    ConvertAction(onSuccess, kv.Key),
                    ConvertAction(onFailure, kv.Key),
                    ConvertAction(onRateLimitChanged, kv.Key)),
                comparer);
            
            _instanceCount = instanceCount;
            _onInstanceCountChanged = onInstanceCountChanged;
        }
        
        public bool CanExecute(TK key, int count = 1)
        {
            return GetRateLimiterSingle(key).CanExecute(count);
        }

        public RateLimit GetRateLimit(TK key, RateLimitType type = RateLimitType.Global)
        {
            return GetRateLimiterSingle(key).GetRateLimit(type);
        }

        public void SetRateLimit(TK key, RateLimit rateLimit)
        {
            GetRateLimiterSingle(key).SetRateLimit(rateLimit);
        }

        public int InstanceCount
        {
            get => _instanceCount;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(InstanceCount));
                
                lock (_updateSettingsLock)
                {
                    if (value == _instanceCount)
                        return;
                    
                    var oldInstanceCount = _instanceCount;
                    
                    foreach (var rateLimiter in _rateLimiters.Values)
                        rateLimiter.InstanceCount = value;
                    
                    _instanceCount = value;
                    
                    _onInstanceCountChanged?.Invoke(new InstanceCountChangedNotification(oldInstanceCount, value));
                }
            }
        }

        private IRateLimiter GetRateLimiterSingle(TK key)
        {
            return _rateLimiters.TryGetValue(key, out var rateLimiter)
                ? rateLimiter
                : throw new Exception($"No rate limit defined for key '{key}'");
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