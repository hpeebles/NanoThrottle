namespace NanoThrottle.Multi
{
    public interface IRateLimiter<in TK>
    {
        string Name { get; }
        
        bool CanExecute(TK key, int count = 1);

        RateLimit GetRateLimit(TK key);
        
        void SetRateLimit(TK key, RateLimit rateLimit);
    }
}