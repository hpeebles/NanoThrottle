using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace NanoThrottle.Tests
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
    }
}