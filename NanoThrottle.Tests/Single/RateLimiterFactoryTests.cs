using System;
using System.Collections.Generic;
using FluentAssertions;
using NanoThrottle.Single;
using Xunit;

namespace NanoThrottle.Tests.Single
{
    public class RateLimiterFactoryTests
    {
        [Fact]
        public void NameIsSetCorrectly()
        {
            var name = Guid.NewGuid().ToString();

            var rateLimiter = RateLimiterFactory
                .Create(name)
                .WithRateLimit(new RateLimit(1, TimeSpan.FromSeconds(1)))
                .Build();

            rateLimiter.Name.Should().Be(name);
        }

        [Fact]
        public void RateLimitsAreSetCorrectly()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));

            var rateLimiter = RateLimiterFactory
                .Create("test")
                .WithRateLimit(rateLimit)
                .Build();
            
            rateLimiter.RateLimit.Should().Be(rateLimit);
        }

        [Fact]
        public void OnSuccessIsSetCorrectly()
        {
            var successCount = 0;

            Action onSuccess = () => successCount++;
            
            var rateLimiter = RateLimiterFactory
                .Create("test")
                .WithRateLimit(new RateLimit(1, TimeSpan.FromSeconds(1)))
                .OnSuccess(onSuccess)
                .Build();
            
            rateLimiter.CanExecute().Should().BeTrue();

            successCount.Should().Be(1);
        }
        
        [Fact]
        public void OnFailureIsSetCorrectly()
        {
            var failureCount = 0;

            Action onfailure = () => failureCount++;
            
            var rateLimiter = RateLimiterFactory
                .Create("test")
                .WithRateLimit(new RateLimit(0, TimeSpan.FromSeconds(1)))
                .OnFailure(onfailure)
                .Build();
            
            rateLimiter.CanExecute().Should().BeFalse();

            failureCount.Should().Be(1);
        }

        [Fact]
        public void OnRateLimitChangedIsSetCorrectly()
        {
            var rateLimitChanges = new List<RateLimitChangedNotification>();

            Action<RateLimitChangedNotification> onRateLimitChanged = rateLimitChanges.Add;
            
            var rateLimit1 = new RateLimit(1, TimeSpan.FromSeconds(1));
            var rateLimit2 = new RateLimit(2, TimeSpan.FromMinutes(1));

            var rateLimiter = RateLimiterFactory
                .Create("test")
                .WithRateLimit(rateLimit1)
                .OnRateLimitChanged(onRateLimitChanged)
                .Build();

            rateLimiter.RateLimit = rateLimit2;

            rateLimitChanges.Should().HaveCount(1);
        }
    }
}