using System;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace NanoThrottle.Tests
{
    public class TokenBucketRateLimiterTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void TokensReplenishAtCorrectRate(int requestsPerSecond)
        {
            var rateLimit = new RateLimit(requestsPerSecond, TimeSpan.FromSeconds(1), 1);
            
            var rateLimiter = new TokenBucketRateLimiter(rateLimit);

            var intervalForOneTokenToBeReplenished = TimeSpan.FromSeconds(1d / requestsPerSecond);
            
            rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
            
            // Wait a little bit less than the expected time for a new token to be replenished
            Thread.Sleep(intervalForOneTokenToBeReplenished * 0.9);

            rateLimiter.CanExecute().Should().BeFalse();
            
            // Wait the remainder of the time, so that now a single token should be available
            Thread.Sleep(intervalForOneTokenToBeReplenished * 0.1);

            rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void MaxBurstIsAdheredTo(int maxBurst)
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1), maxBurst);
            
            var rateLimiter = new TokenBucketRateLimiter(rateLimit);

            for (var i = 0; i < maxBurst; i++)
                rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(100)]
        public void RequestingHigherCountUsesUpMoreTokens(int count)
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1), 1000);
            
            var rateLimiter = new TokenBucketRateLimiter(rateLimit);

            for (var i = 0; i < 1000 / count; i++)
                rateLimiter.CanExecute(count).Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
    }
}