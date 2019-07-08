using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace NanoThrottle.Tests
{
    public class RateLimiterMultiTests
    {
        [Fact]
        public void KeepsRateLimitersSeparate()
        {
            var rateLimiterMulti = new RateLimiterMulti<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1))),
                new KeyValuePair<int, RateLimit>(2, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            rateLimiterMulti.CanExecute(1).Should().BeTrue();
            rateLimiterMulti.CanExecute(1).Should().BeFalse();

            rateLimiterMulti.CanExecute(2).Should().BeTrue();
            rateLimiterMulti.CanExecute(2).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(100)]
        public void RequestingHigherCountUsesUpMoreTokens(int count)
        {
            var rateLimiterMulti = new RateLimiterMulti<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1000, TimeSpan.MaxValue))
            });
            
            for (var i = 0; i < 1000 / count; i++)
                rateLimiterMulti.CanExecute(1, count).Should().BeTrue();

            rateLimiterMulti.CanExecute(1).Should().BeFalse();
        }
        
        [Fact]
        public void ThrowsIfKeyNotFound()
        {
            var rateLimiterMulti = new RateLimiterMulti<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            rateLimiterMulti.CanExecute(1).Should().BeTrue();

            Func<bool> func = () => rateLimiterMulti.CanExecute(2);

            func.Should().Throw<Exception>();
        }
    }
}