using System;
using FluentAssertions;
using Xunit;

namespace NanoThrottle.Tests
{
    public class RateLimiterSingleFactoryTests
    {
        [Fact]
        public void NameIsSetCorrectly()
        {
            var name = Guid.NewGuid().ToString();

            var rateLimiter = RateLimiterSingleFactory
                .Create(name)
                .WithRateLimit(new RateLimit(1, TimeSpan.FromSeconds(1)))
                .Build();

            rateLimiter.Name.Should().Be(name);
        }

        [Fact]
        public void RateLimitsAreSetCorrectly()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));

            var rateLimiter = RateLimiterSingleFactory
                .Create("test")
                .WithRateLimit(rateLimit)
                .Build();
            
            rateLimiter.RateLimit.Should().Be(rateLimit);
        }
    }
}