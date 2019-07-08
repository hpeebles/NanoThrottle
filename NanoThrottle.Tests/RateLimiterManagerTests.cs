using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace NanoThrottle.Tests
{
    public class RateLimiterManagerTests
    {
        [Fact]
        public void KeepsRateLimitersSeparate()
        {
            var rateLimiterManager = new RateLimiterManager<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1))),
                new KeyValuePair<int, RateLimit>(2, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            rateLimiterManager.CanExecute(1).Should().BeTrue();
            rateLimiterManager.CanExecute(1).Should().BeFalse();

            rateLimiterManager.CanExecute(2).Should().BeTrue();
            rateLimiterManager.CanExecute(2).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(100)]
        public void RequestingHigherCountUsesUpMoreTokens(int count)
        {
            var rateLimiterManager = new RateLimiterManager<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1000, TimeSpan.MaxValue))
            });
            
            for (var i = 0; i < 1000 / count; i++)
                rateLimiterManager.CanExecute(1, count).Should().BeTrue();

            rateLimiterManager.CanExecute(1).Should().BeFalse();
        }
        
        [Fact]
        public void ThrowsIfKeyNotFound()
        {
            var rateLimiterManager = new RateLimiterManager<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
            });

            rateLimiterManager.CanExecute(1).Should().BeTrue();

            Func<bool> func = () => rateLimiterManager.CanExecute(2);

            func.Should().Throw<Exception>();
        }
    }
}