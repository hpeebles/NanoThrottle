using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using NanoThrottle.Redis;
using Xunit;

namespace NanoThrottle.Tests.Redis
{
    public class InstanceCountObservableFactoryTests
    {
        [Fact]
        public void InstanceCountIsIncrementedWhenSubscribersConnect()
        {
            const int maxSubscriptionsCount = 5;
            
            var counts = new int[maxSubscriptionsCount];

            var key = Guid.NewGuid().ToString();
            var redisConfig = new RedisConfiguration(TestConnectionString.Value);

            CountdownEvent countdown;
            for (var i = 0; i < maxSubscriptionsCount; i++)
            {
                countdown = new CountdownEvent(i + 1);
                var index = i;
                InstanceCountObservableFactory
                    .Create(redisConfig, key, 1, Int32.MaxValue, TimeSpan.FromMilliseconds(10))
                    .Subscribe(count =>
                    {
                        counts[index] = count;
                        countdown.Signal();
                    });

                Wait(countdown);
                
                for (var j = 0; j <= i; j++)
                    counts[j].Should().Be(i + 1);
            }
        }
        
        [Fact]
        public void InstanceCountIsDecrementedWhenSubscribersDisconnect()
        {
            const int maxSubscriptionsCount = 5;
            
            var subscriptions = new List<IDisposable>();
            var counts = new int[maxSubscriptionsCount];

            var key = Guid.NewGuid().ToString();
            var redisConfig = new RedisConfiguration(TestConnectionString.Value);

            CountdownEvent countdown;
            for (var i = 0; i < maxSubscriptionsCount; i++)
            {
                countdown = new CountdownEvent(i + 1);
                
                var index = i;
                subscriptions.Add(InstanceCountObservableFactory
                    .Create(redisConfig, key, 1, Int32.MaxValue, TimeSpan.FromMilliseconds(10))
                    .Subscribe(count =>
                    {
                        counts[index] = count;
                        countdown.Signal();
                    }));
                
                Wait(countdown);
            }
            
            for (var i = 0; i < maxSubscriptionsCount - 1; i++)
            {
                countdown = new CountdownEvent(maxSubscriptionsCount - 1 - i);
                
                subscriptions[i].Dispose();

                Wait(countdown);

                for (var j = i + 1; j < maxSubscriptionsCount - 1; j++)
                    counts[j].Should().Be(maxSubscriptionsCount - 1 - i);
            }
        }

        private static void Wait(CountdownEvent countdown)
        {
            if (!countdown.Wait(TimeSpan.FromSeconds(5)))
                throw new Exception($"Failed to wait for all events to occur. Initial count:{countdown.InitialCount}. CurrentCount:{countdown.CurrentCount}");
        }
    }
}