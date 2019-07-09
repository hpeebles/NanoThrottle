namespace NanoThrottle
{
    public interface IRateLimiterSingle
    {
        bool CanExecute(int count = 1);

        RateLimit RateLimit { get; set; }
    }
}