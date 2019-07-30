using System;
using System.Collections.Generic;
using FluentAssertions;
using NanoThrottle.Multi;
using Xunit;

namespace NanoThrottle.Tests.Multi
{
    public class RateLimiterTests
    {
        [Theory]
        [InlineData("123")]
        [InlineData("abc")]
        public void NameSetCorrectly(string name)
        {
            var rateLimiter = new RateLimiter<int>(name, new[]
            {
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            rateLimiter.Name.Should().Be(name);
        }
        
        [Fact]
        public void KeepsRateLimitersSeparate()
        {
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1))),
                new KeyValuePair<int, RateLimit>(2, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            rateLimiter.CanExecute(1).Should().BeTrue();
            rateLimiter.CanExecute(1).Should().BeFalse();

            rateLimiter.CanExecute(2).Should().BeTrue();
            rateLimiter.CanExecute(2).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(100)]
        public void RequestingHigherCountUsesUpMoreTokens(int count)
        {
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1000, TimeSpan.MaxValue))
            });
            
            for (var i = 0; i < 1000 / count; i++)
                rateLimiter.CanExecute(1, count).Should().BeTrue();

            rateLimiter.CanExecute(1).Should().BeFalse();
        }
        
        [Fact]
        public void ThrowsIfKeyNotFound()
        {
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            rateLimiter.CanExecute(1).Should().BeTrue();

            Func<bool> func = () => rateLimiter.CanExecute(2);

            func.Should().Throw<Exception>();
        }

        [Fact]
        public void CanGetRateLimit()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, rateLimit)
            });

            rateLimiter.GetRateLimit(1).Should().Be(rateLimit);
        }
        
        [Fact]
        public void CanSetRateLimit()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, rateLimit)
            });

            var newRateLimit = new RateLimit(2, TimeSpan.FromMinutes(1));
            
            rateLimiter.SetRateLimit(1, newRateLimit);

            rateLimiter.GetRateLimit(1).Should().Be(newRateLimit);
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void CanGetInstanceCount(int instanceCount)
        {
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
            }, instanceCount);

            rateLimiter.InstanceCount.Should().Be(instanceCount);
        }
        
        [Fact]
        public void CanSetInstanceCount()
        {
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            for (var count = 1; count < 10; count++)
            {
                rateLimiter.InstanceCount = count;
                rateLimiter.InstanceCount.Should().Be(count);
            }
        }

        [Fact]
        public void OnSuccessIsTriggeredCorrectly()
        {
            var successList = new List<int>();

            Action<int> onSuccess = successList.Add;
            
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1))),
                new KeyValuePair<int, RateLimit>(2, new RateLimit(2, TimeSpan.FromSeconds(1)))
            }, onSuccess: onSuccess);

            rateLimiter.CanExecute(1).Should().BeTrue();

            successList.Should().BeEquivalentTo(1);

            rateLimiter.CanExecute(1).Should().BeFalse();

            successList.Should().BeEquivalentTo(1);

            rateLimiter.CanExecute(2).Should().BeTrue();

            successList.Should().BeEquivalentTo(1, 2);

            rateLimiter.CanExecute(2).Should().BeTrue();

            successList.Should().BeEquivalentTo(1, 2, 2);
            
            rateLimiter.CanExecute(2).Should().BeFalse();

            successList.Should().BeEquivalentTo(1, 2, 2);
        }
        
        [Fact]
        public void OnFailureIsTriggeredCorrectly()
        {
            var failureList = new List<int>();

            Action<int> onFailure = failureList.Add;
            
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1))),
                new KeyValuePair<int, RateLimit>(2, new RateLimit(2, TimeSpan.FromSeconds(1)))
            }, onFailure: onFailure);

            rateLimiter.CanExecute(1).Should().BeTrue();

            failureList.Should().BeEmpty();

            rateLimiter.CanExecute(1).Should().BeFalse();

            failureList.Should().BeEquivalentTo(1);

            rateLimiter.CanExecute(2).Should().BeTrue();

            failureList.Should().BeEquivalentTo(1);

            rateLimiter.CanExecute(2).Should().BeTrue();

            failureList.Should().BeEquivalentTo(1);
            
            rateLimiter.CanExecute(2).Should().BeFalse();

            failureList.Should().BeEquivalentTo(1, 2);
        }
        
        [Fact]
        public void OnRateLimitChangedIsTriggeredCorrectly()
        {
            var rateLimitChanges = new List<RateLimitChangedNotification<int>>();

            Action<RateLimitChangedNotification<int>> onRateLimitChanged = rateLimitChanges.Add;
            
            var rateLimit1 = new RateLimit(1, TimeSpan.FromSeconds(1));
            var rateLimit2 = new RateLimit(2, TimeSpan.FromMinutes(1));
            
            var rateLimiter = new RateLimiter<int>("test", new[]
            {    
                new KeyValuePair<int, RateLimit>(1, rateLimit1),
                new KeyValuePair<int, RateLimit>(2, rateLimit2)
            }, onRateLimitChanged: onRateLimitChanged);

            rateLimiter.SetRateLimit(1, rateLimit1);

            rateLimitChanges.Should().BeEmpty();
            
            rateLimiter.SetRateLimit(1, rateLimit2);

            var expected1 = new RateLimitChangedNotification<int>(
                1,
                rateLimit1.AsLocal(1),
                rateLimit2.AsLocal(1),
                rateLimit1,
                rateLimit2);
            
            rateLimitChanges.Should().ContainSingle()
                .And.BeEquivalentTo(expected1);
            
            rateLimiter.SetRateLimit(2, rateLimit1);

            var expected2 = new RateLimitChangedNotification<int>(
                2,
                rateLimit2.AsLocal(1),
                rateLimit1.AsLocal(1),
                rateLimit2,
                rateLimit1);
            
            rateLimitChanges.Should().HaveCount(2)
                .And.HaveElementAt(1, expected2);
        }
    }
}