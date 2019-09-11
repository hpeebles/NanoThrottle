namespace NanoThrottle.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            Single_RateLimiter.Run();
            Multi_RateLimiter.Run();
        }
    }
}