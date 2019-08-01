using System;
using System.Reactive.Subjects;
using FluentAssertions;
using NanoThrottle.Single;
using Xunit;

namespace NanoThrottle.Tests.Single
{
    public class RateLimiterFactoryExtensionsTests
    {
        [Fact]
        public void WithRateLimitUpdatesWorksCorrectly()
        {
            var updates = new Subject<RateLimit>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(new RateLimit(0, TimeSpan.FromSeconds(1)))
                .WithRateLimitUpdates(updates)
                .Build();

            for (var i = 0; i < 10; i++)
            {
                var rateLimit = new RateLimit(i, TimeSpan.FromSeconds(1));
                
                updates.OnNext(rateLimit);

                rateLimiter.GetRateLimit().Should().Be(rateLimit);
            }
        }
        
        [Fact]
        public void WithInstanceCountUpdatesWorksCorrectly()
        {
            var updates = new Subject<int>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(new RateLimit(100, TimeSpan.FromSeconds(1)))
                .WithInstanceCountUpdates(updates)
                .Build();

            for (var count = 2; count < 10; count++)
            {
                updates.OnNext(count);

                rateLimiter.InstanceCount.Should().Be(count);
            }
        }
    }
}