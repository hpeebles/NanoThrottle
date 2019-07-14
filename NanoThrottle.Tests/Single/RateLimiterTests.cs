using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using NanoThrottle.Single;
using Xunit;

namespace NanoThrottle.Tests.Single
{
    public class RateLimiterTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void TokensReplenishAtCorrectRate(int requestsPerSecond)
        {
            var rateLimit = new RateLimit(requestsPerSecond, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter("test", rateLimit);

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
            
            var rateLimiter = new RateLimiter("test", rateLimit);

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
            
            var rateLimiter = new RateLimiter("test", rateLimit);

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
            
            var rateLimiter = new RateLimiter(name, rateLimit);

            rateLimiter.Name.Should().Be(name);
        }

        [Fact]
        public void CanSetRateLimit()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter("test", rateLimit);

            rateLimiter.RateLimit.Should().Be(rateLimit);

            var newRateLimit = new RateLimit(2, TimeSpan.FromMinutes(1));
            
            rateLimiter.RateLimit = newRateLimit;

            rateLimiter.RateLimit.Should().Be(newRateLimit);
        }

        [Fact]
        public void NewRateLimitTakesEffectAfterUpdate()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter("test", rateLimit);

            rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
            
            rateLimiter.RateLimit = new RateLimit(10, TimeSpan.FromSeconds(1));
            
            Thread.Sleep(TimeSpan.FromSeconds(1));

            for (var i = 0; i < 10; i++)
                rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
        
        [Fact]
        public void OnSuccessIsTriggeredCorrectly()
        {
            var successCount = 0;

            Action onSuccess = () => successCount++;
            
            var rateLimiter = new RateLimiter("test", new RateLimit(1, TimeSpan.FromSeconds(1)), onSuccess);

            rateLimiter.CanExecute().Should().BeTrue();

            successCount.Should().Be(1);

            rateLimiter.CanExecute().Should().BeFalse();
            
            successCount.Should().Be(1);
        }

        [Fact]
        public void OnFailureIsTriggeredCorrectly()
        {
            var failureCount = 0;

            Action onfailure = () => failureCount++;

            var rateLimiter = new RateLimiter("test", new RateLimit(1, TimeSpan.FromSeconds(1)), onFailure: onfailure);

            rateLimiter.CanExecute().Should().BeTrue();

            failureCount.Should().Be(0);

            rateLimiter.CanExecute().Should().BeFalse();

            failureCount.Should().Be(1);
        }
        
        [Fact]
        public void OnRateLimitChangedIsTriggeredCorrectly()
        {
            var rateLimitChanges = new List<RateLimitChangedNotification>();

            Action<RateLimitChangedNotification> onRateLimitChanged = rateLimitChanges.Add;
            
            var rateLimit1 = new RateLimit(1, TimeSpan.FromSeconds(1));
            var rateLimit2 = new RateLimit(2, TimeSpan.FromMinutes(1));
            
            var rateLimiter = new RateLimiter("test", rateLimit1, onRateLimitChanged: onRateLimitChanged);

            rateLimiter.RateLimit = rateLimit1;

            rateLimitChanges.Should().BeEmpty();
            
            rateLimiter.RateLimit = rateLimit2;

            rateLimitChanges.Should().ContainSingle()
                .And.BeEquivalentTo(new RateLimitChangedNotification(rateLimit1, rateLimit2));
            
            rateLimiter.RateLimit = rateLimit1;

            rateLimitChanges.Should().HaveCount(2)
                .And.HaveElementAt(1, new RateLimitChangedNotification(rateLimit2, rateLimit1));
        }
    }
}