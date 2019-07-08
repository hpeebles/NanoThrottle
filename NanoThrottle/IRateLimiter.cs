namespace NanoThrottle
{
    public interface IRateLimiter
    {
        bool CanExecute(int count = 1);
    }
}