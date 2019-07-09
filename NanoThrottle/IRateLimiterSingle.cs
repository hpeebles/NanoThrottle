namespace NanoThrottle
{
    public interface IRateLimiterSingle
    {
        string Name { get; }

        RateLimit RateLimit { get; set; }
        
        bool CanExecute(int count = 1);
    }
}