using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace NanoThrottle.Tests
{
    public class RateLimiterTests
    {
        [Fact]
        public void KeepsRateLimitersSeparate()
        {
            var rateLimiter = new RateLimiter<int>(new[]
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
            var rateLimiter = new RateLimiter<int>(new[]
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
            var rateLimiter = new RateLimiter<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            rateLimiter.CanExecute(1).Should().BeTrue();

            Func<bool> func = () => rateLimiter.CanExecute(2);

            func.Should().Throw<Exception>();
        }
    }
}