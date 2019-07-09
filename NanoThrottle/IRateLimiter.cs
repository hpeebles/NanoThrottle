namespace NanoThrottle
{
    public interface IRateLimiter<in TK>
    {
        bool CanExecute(TK key, int count = 1);

        RateLimit GetRateLimit(TK key);
        
        void SetRateLimit(TK key, RateLimit rateLimit);
    }
}