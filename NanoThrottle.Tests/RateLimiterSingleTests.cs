using System;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace NanoThrottle.Tests
{
    public class RateLimiterSingleTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void TokensReplenishAtCorrectRate(int requestsPerSecond)
        {
            var rateLimit = new RateLimit(requestsPerSecond, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiterSingle("test", rateLimit);

            var intervalForOneTokenToBeReplenished = TimeSpan.FromSeconds(1d / requestsPerSecond);

            // Use up all the starting tokens
            while (rateLimiter.CanExecute())
            { }

            // Wait a little bit less than the expected time for a new token to be replenished
            Thread.Sleep(intervalForOneTokenToBeReplenished * 0.8);

            rateLimiter.CanExecute().Should().BeFalse();
            
            // Wait the remainder of the time, so that now a single token should be available
            Thread.Sleep(intervalForOneTokenToBeReplenished * 0.2);

            rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void CountDeterminesTheMaxBurst(int count)
        {
            var rateLimit = new RateLimit(count, TimeSpan.MaxValue);
            
            var rateLimiter = new RateLimiterSingle("test", rateLimit);

            for (var i = 0; i < count; i++)
                rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(100)]
        public void RequestingHigherCountUsesUpMoreTokens(int count)
        {
            var rateLimit = new RateLimit(1000, TimeSpan.MaxValue);
            
            var rateLimiter = new RateLimiterSingle("test", rateLimit);

            for (var i = 0; i < 1000 / count; i++)
                rateLimiter.CanExecute(count).Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
        
        [Theory]
        [InlineData("123")]
        [InlineData("abc")]
        public void NameSetCorrectly(string name)
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiterSingle(name, rateLimit);

            rateLimiter.Name.Should().Be(name);
        }

        [Fact]
        public void CanSetRateLimit()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiterSingle("test", rateLimit);

            rateLimiter.RateLimit.Should().Be(rateLimit);

            var newRateLimit = new RateLimit(2, TimeSpan.FromMinutes(1));
            
            rateLimiter.RateLimit = newRateLimit;

            rateLimiter.RateLimit.Should().Be(newRateLimit);
        }

        [Fact]
        public void NewRateLimitTakesEffectAfterUpdate()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiterSingle("test", rateLimit);

            rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
            
            rateLimiter.RateLimit = new RateLimit(10, TimeSpan.FromSeconds(1));
            
            Thread.Sleep(TimeSpan.FromSeconds(1));

            for (var i = 0; i < 10; i++)
                rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
    }
}