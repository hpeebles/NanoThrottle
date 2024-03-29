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
        public void RateLimitsAreSetCorrectly()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));

            var rateLimiter = RateLimiter
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, rateLimit)
                })
                .Build();

            rateLimiter.GetRateLimit(1).Should().Be(rateLimit);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void InstanceCountIsSetCorrectly(int instanceCount)
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));

            var rateLimiter = RateLimiter
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, rateLimit)
                })
                .WithInstanceCount(instanceCount)
                .Build();

            rateLimiter.InstanceCount.Should().Be(instanceCount);
        }

        [Fact]
        public void KeyComparerIsSetCorrectly()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));

            var mockComparer = new Mock<IEqualityComparer<int>>();

            mockComparer.Setup(x => x.Equals(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
            
            var rateLimiter = RateLimiter
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

        [Fact]
        public void OnSuccessIsSetCorrectly()
        {
            var successList = new List<int>();
            
            Action<int> onSuccess = successList.Add;
            
            var rateLimiter = RateLimiter
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
                })
                .OnSuccess(onSuccess)
                .Build();

            rateLimiter.CanExecute(1).Should().BeTrue();

            successList.Should().BeEquivalentTo(1);
        }
        
        [Fact]
        public void OnFailureIsSetCorrectly()
        {
            var failureList = new List<int>();
            
            Action<int> onFailure = failureList.Add;
            
            var rateLimiter = RateLimiter
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, new RateLimit(0, TimeSpan.FromSeconds(1)))
                })
                .OnFailure(onFailure)
                .Build();

            rateLimiter.CanExecute(1).Should().BeFalse();

            failureList.Should().BeEquivalentTo(1);
        }
        
        [Fact]
        public void OnRateLimitChangedIsSetCorrectly()
        {
            var rateLimitChanges = new List<RateLimitChangedNotification<int>>();

            Action<RateLimitChangedNotification<int>> onRateLimitChanged = rateLimitChanges.Add;
            
            var rateLimit1 = new RateLimit(1, TimeSpan.FromSeconds(1));
            var rateLimit2 = new RateLimit(2, TimeSpan.FromMinutes(1));

            var rateLimiter = RateLimiter
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, rateLimit1)
                })
                .OnRateLimitChanged(onRateLimitChanged)
                .Build();

            rateLimiter.SetRateLimit(1, rateLimit2);

            rateLimitChanges.Should().ContainSingle();
        }
        
        [Fact]
        public void OnInstanceCountChangedIsSetCorrectly()
        {
            var instanceCountChanges = new List<InstanceCountChangedNotification>();

            Action<InstanceCountChangedNotification> onInstanceCountChanged = instanceCountChanges.Add;
            
            var rateLimiter = RateLimiter
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, new RateLimit(10, TimeSpan.FromSeconds(1)))
                })
                .OnInstanceCountChanged(onInstanceCountChanged)
                .Build();

            rateLimiter.InstanceCount = 2;

            instanceCountChanges.Should().ContainSingle();
        }
    }
}