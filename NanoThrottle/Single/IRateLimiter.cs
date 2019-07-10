namespace NanoThrottle.Single
{
    public interface IRateLimiter
    {
        string Name { get; }

        RateLimit RateLimit { get; set; }
        
        bool CanExecute(int count = 1);
    }
}