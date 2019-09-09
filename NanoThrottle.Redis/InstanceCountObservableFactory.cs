using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace NanoThrottle.Redis
{
    internal static class InstanceCountObservableFactory
    {
        public static IObservable<int> Create(
            ConfigurationOptions redisConfiguration,
            string key,
            int minInstanceCount,
            int maxInstanceCount,
            TimeSpan refreshInterval)
        {
            return Observable
                .Create<int>(async (x, token) =>
                {
                    var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConfiguration);

                    var subscriber = connectionMultiplexer.GetSubscriber();

                    subscriber.Subscribe(key);

                    token.Register(() =>
                    {
                        subscriber.Unsubscribe(key);
                        connectionMultiplexer.Dispose();
                    });
                    
                    while (!token.IsCancellationRequested)
                    {
                        var instanceCountTasks = connectionMultiplexer
                            .GetEndPoints()
                            .Select(e => connectionMultiplexer.GetServer(e))
                            .Select(s => s.SubscriptionSubscriberCountAsync(key))
                            .ToArray();

                        await Task.WhenAll(instanceCountTasks);

                        var instanceCount = instanceCountTasks.Sum(t => (int) t.Result);

                        if (instanceCount < minInstanceCount)
                            instanceCount = minInstanceCount;
                        else if (instanceCount > maxInstanceCount)
                            instanceCount = maxInstanceCount;

                        x.OnNext(instanceCount);

                        try
                        {
                            await Task.Delay(refreshInterval, token);
                        }
                        catch (TaskCanceledException)
                        { }
                    }
                })
                .DistinctUntilChanged()
                .Retry();
        }
    }
}