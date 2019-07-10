using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NanoThrottle.Multi;
using Xunit;

namespace NanoThrottle.Tests.Multi
{
    public class RateLimiterFactoryTests
    {
        [Fact]
        public void NameIsSetCorrectly()
        {
            var name = Guid.NewGuid().ToString();

            var rateLimiter = RateLimiterFactory
                .Create(name)
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
                })
                .Build();

            rateLimiter.Name.Should().Be(name);
        }

        [Fact]
        public void RateLimitsAreSetCorrectly()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));

            var rateLimiter = RateLimiterFactory
                .Create("test")
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, rateLimit)
                })
                .Build();

            rateLimiter.GetRateLimit(1).Should().Be(rateLimit);
        }

        [Fact]
        public void KeyComparerIsSetCorrectly()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));

            var mockComparer = new Mock<IEqualityComparer<int>>();

            mockComparer.Setup(x => x.Equals(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
            
            var rateLimiter = RateLimiterFactory
                .Create("test")
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, rateLimit)
                })
                .WithKeyComparer(mockComparer.Object)
                .Build();

            var initialCount = mockComparer.Invocations.Count;

            rateLimiter.CanExecute(1);

            mockComparer.Invocations.Count.Should().BeGreaterThan(initialCount);
        }
    }
}