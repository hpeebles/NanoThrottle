using System;
using System.Threading.Tasks;
using FluentAssertions;
using NanoThrottle.Redis;
using Xunit;

namespace NanoThrottle.Tests.Redis
{
    public class InstanceCountObservableFactoryTests
    {
        [Fact]
        public async Task InstanceCountIsCorrect()
        {
            var counts = new int[10];

            var key = Guid.NewGuid().ToString();
            var redisConfig = new RedisConfiguration(TestConnectionString.Value);
            
            for (var i = 0; i < 10; i++)
            {
                var index = i;
                InstanceCountObservableFactory
                    .Create(redisConfig, key, 1, Int32.MaxValue, TimeSpan.FromMilliseconds(100))
                    .Subscribe(count => counts[index] = count);

                await Task.Delay(TimeSpan.FromSeconds(1));
                
                for (var j = 0; j <= i; j++)
                    counts[j].Should().Be(i + 1);
            }
        }
    }
}