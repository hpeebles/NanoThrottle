namespace NanoThrottle
{
    public interface IRateLimiter<in TK>
    {
        bool CanExecute(TK key, int count = 1);
    }
}