using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using FluentAssertions;
using NanoThrottle.Multi;
using Xunit;

namespace NanoThrottle.Tests.Multi
{
    public class RateLimiterFactoryExtensionsTests
    {
        [Fact]
        public void WithRateLimitUpdatesWorksCorrectly()
        {
            var updates = new Subject<KeyValuePair<int, RateLimit>>();
            
            var rateLimiter = RateLimiter
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, new RateLimit(0, TimeSpan.FromSeconds(1)))
                })
                .WithRateLimitUpdates(updates)
                .Build();

            for (var i = 0; i < 10; i++)
            {
                var rateLimit = new RateLimit(i, TimeSpan.FromSeconds(1));
                
                updates.OnNext(new KeyValuePair<int, RateLimit>(1, rateLimit));

                rateLimiter.GetRateLimit(1).Should().Be(rateLimit);
            }
        }
    }
}