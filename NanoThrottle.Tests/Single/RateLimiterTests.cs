using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using FluentAssertions;
using NanoThrottle.Single;
using Xunit;

namespace NanoThrottle.Tests.Single
{
    public class RateLimiterTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void TokensReplenishAtCorrectRate(int requestsPerSecond)
        {
            var rateLimit = new RateLimit(requestsPerSecond, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter(rateLimit);

            var intervalForOneTokenToBeReplenished = TimeSpan.FromSeconds(1d / requestsPerSecond);

            // Use up all the starting tokens
            while (rateLimiter.CanExecute())
            { }

            // Wait a little bit less than the expected time for a new token to be replenished
            Thread.Sleep(intervalForOneTokenToBeReplenished * 0.8);

            rateLimiter.CanExecute().Should().BeFalse();
            
            // Wait the remainder of the time, so that now a single token should be available
            Thread.Sleep(intervalForOneTokenToBeReplenished * 0.2);

            rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void CountDeterminesTheMaxBurst(int count)
        {
            var rateLimit = new RateLimit(count, TimeSpan.MaxValue);
            
            var rateLimiter = new RateLimiter(rateLimit);

            for (var i = 0; i < count; i++)
                rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(100)]
        public void RequestingHigherCountUsesUpMoreTokens(int count)
        {
            var rateLimit = new RateLimit(1000, TimeSpan.MaxValue);
            
            var rateLimiter = new RateLimiter(rateLimit);

            for (var i = 0; i < 1000 / count; i++)
                rateLimiter.CanExecute(count).Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
        
        [Fact]
        public void CanSetRateLimit()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter(rateLimit);

            rateLimiter.GetRateLimit().Should().Be(rateLimit);

            var newRateLimit = new RateLimit(2, TimeSpan.FromMinutes(1));
            
            rateLimiter.SetRateLimit(newRateLimit);

            rateLimiter.GetRateLimit().Should().Be(newRateLimit);
        }

        [Fact]
        public void NewRateLimitTakesEffectAfterUpdate()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter(rateLimit);

            rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
            
            rateLimiter.SetRateLimit(new RateLimit(10, TimeSpan.FromSeconds(1)));
            
            Thread.Sleep(TimeSpan.FromSeconds(1));

            for (var i = 0; i < 10; i++)
                rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }
        
        [Fact]
        public void OnSuccessIsTriggeredCorrectly()
        {
            var successCount = 0;

            Action onSuccess = () => successCount++;
            
            var rateLimiter = new RateLimiter(new RateLimit(1, TimeSpan.FromSeconds(1)), onSuccess: onSuccess);

            rateLimiter.CanExecute().Should().BeTrue();

            successCount.Should().Be(1);

            rateLimiter.CanExecute().Should().BeFalse();
            
            successCount.Should().Be(1);
        }

        [Fact]
        public void OnFailureIsTriggeredCorrectly()
        {
            var failureCount = 0;

            Action onfailure = () => failureCount++;

            var rateLimiter = new RateLimiter(new RateLimit(1, TimeSpan.FromSeconds(1)), onFailure: onfailure);

            rateLimiter.CanExecute().Should().BeTrue();

            failureCount.Should().Be(0);

            rateLimiter.CanExecute().Should().BeFalse();

            failureCount.Should().Be(1);
        }
        
        [Fact]
        public void OnRateLimitChangedIsTriggeredCorrectly()
        {
            var rateLimitChanges = new List<RateLimitChangedNotification>();

            Action<RateLimitChangedNotification> onRateLimitChanged = rateLimitChanges.Add;
            
            var rateLimit1 = new RateLimit(1, TimeSpan.FromSeconds(1));
            var rateLimit2 = new RateLimit(2, TimeSpan.FromMinutes(1));
            
            var rateLimiter = new RateLimiter(rateLimit1, onRateLimitChanged: onRateLimitChanged);

            rateLimiter.SetRateLimit(rateLimit1);

            rateLimitChanges.Should().BeEmpty();
            
            rateLimiter.SetRateLimit(rateLimit2);

            var expected1 = new RateLimitChangedNotification(
                rateLimit1.AsLocal(1),
                rateLimit2.AsLocal(1),
                rateLimit1,
                rateLimit2);
            
            rateLimitChanges.Should().ContainSingle()
                .And.BeEquivalentTo(expected1);
            
            rateLimiter.SetRateLimit(rateLimit1);

            var expected2 = new RateLimitChangedNotification(
                rateLimit2.AsLocal(1),
                rateLimit1.AsLocal(1),
                rateLimit2,
                rateLimit1);
            
            rateLimitChanges.Should().HaveCount(2)
                .And.HaveElementAt(1, expected2);
        }
        
        [Fact]
        public void OnInstanceCountChangedIsTriggeredCorrectly()
        {
            var instanceCountChanges = new List<InstanceCountChangedNotification>();

            Action<InstanceCountChangedNotification> onInstanceCountChanged = instanceCountChanges.Add;

            var rateLimiter = new RateLimiter(
                new RateLimit(10, TimeSpan.FromSeconds(1)),
                onInstanceCountChanged: onInstanceCountChanged);
            
            for (var i = 1; i < 10; i++)
            {
                rateLimiter.InstanceCount = i;

                if (i == 1)
                {
                    instanceCountChanges.Should().BeEmpty();
                }
                else
                {
                    var expected = new InstanceCountChangedNotification(i - 1, i);

                    instanceCountChanges.Should().HaveCount(i - 1)
                        .And.HaveElementAt(i - 2, expected);
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void SettingInstanceCountAffectsLocalRateLimit(int instanceCount)
        {
            var rateLimit = new RateLimit(10, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter(rateLimit, instanceCount);

            rateLimiter.GetRateLimit().Should().Be(rateLimit);
            
            var localRateLimit = new RateLimit(10 / instanceCount, TimeSpan.FromSeconds(1), RateLimitType.Local);
            
            rateLimiter.GetRateLimit(RateLimitType.Local).Should().Be(localRateLimit);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void LocalRateLimitTakesEffectLocally(int instanceCount)
        {
            var globalRateLimit = new RateLimit(100, TimeSpan.FromHours(1));
            
            var rateLimiter = new RateLimiter(globalRateLimit, instanceCount);

            for (var i = 0; i < 100 / instanceCount; i++)
                rateLimiter.CanExecute().Should().BeTrue();

            rateLimiter.CanExecute().Should().BeFalse();
        }

        [Fact]
        public void UpdatingInstanceCountUpdatesLocalRateLimit()
        {
            var globalRateLimit = new RateLimit(100, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter(globalRateLimit);

            rateLimiter.GetRateLimit().Should().Be(globalRateLimit);
            rateLimiter.GetRateLimit(RateLimitType.Local).Should().Be(
                new RateLimit(100, TimeSpan.FromSeconds(1), RateLimitType.Local));

            rateLimiter.InstanceCount = 2;

            rateLimiter.GetRateLimit().Should().Be(globalRateLimit);
            rateLimiter.GetRateLimit(RateLimitType.Local).Should().Be(
                new RateLimit(50, TimeSpan.FromSeconds(1), RateLimitType.Local));

            rateLimiter.InstanceCount = 10;
            
            rateLimiter.GetRateLimit().Should().Be(globalRateLimit);
            rateLimiter.GetRateLimit(RateLimitType.Local).Should().Be(
                new RateLimit(10, TimeSpan.FromSeconds(1), RateLimitType.Local));
        }
        
        [Fact]
        public void WithRateLimitUpdatesWorksCorrectly()
        {
            var updates = new Subject<RateLimit>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(updates)
                .Build();

            for (var i = 0; i < 10; i++)
            {
                var rateLimit = new RateLimit(i, TimeSpan.FromSeconds(1));
                
                updates.OnNext(rateLimit);

                rateLimiter.GetRateLimit().Should().Be(rateLimit);
            }
        }
        
        [Fact]
        public void WithInstanceCountUpdatesWorksCorrectly()
        {
            var updates = new Subject<int>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(new RateLimit(100, TimeSpan.FromSeconds(1)))
                .WithInstanceCount(updates)
                .Build();

            for (var count = 2; count < 10; count++)
            {
                updates.OnNext(count);

                rateLimiter.InstanceCount.Should().Be(count);
            }
        }

        [Fact]
        public void ThrowsIfNotInitialized()
        {
            var updates = new Subject<RateLimit>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(updates)
                .Build();

            Action action = () => rateLimiter.CanExecute();

            action.Should().Throw<InvalidOperationException>();
        }
        
        [Fact]
        public void ThrowsIfDisposed()
        {
            var updates = new Subject<RateLimit>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(updates)
                .Build();

            rateLimiter.Dispose();
            
            Action action = () => rateLimiter.CanExecute();

            action.Should().Throw<ObjectDisposedException>();
        }
        
        [Fact]
        public void SubscribesToUpdatesImmediately()
        {
            var rateLimitUpdates = new Subject<RateLimit>();
            var instanceCountUpdates = new Subject<int>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(rateLimitUpdates)
                .WithInstanceCount(instanceCountUpdates)
                .Build();

            rateLimitUpdates.HasObservers.Should().BeTrue();
            instanceCountUpdates.HasObservers.Should().BeTrue();
        }
        
        [Fact]
        public void UnsubscribesFromUpdatesOnDispose()
        {
            var rateLimitUpdates = new Subject<RateLimit>();
            var instanceCountUpdates = new Subject<int>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(rateLimitUpdates)
                .WithInstanceCount(instanceCountUpdates)
                .Build();

            rateLimitUpdates.HasObservers.Should().BeTrue();
            instanceCountUpdates.HasObservers.Should().BeTrue();
            
            rateLimiter.Dispose();
            
            rateLimitUpdates.HasObservers.Should().BeFalse();
            instanceCountUpdates.HasObservers.Should().BeFalse();
        }
        
        [Fact]
        public void StateIsUpdatedCorrectly()
        {
            var updates = new Subject<RateLimit>();

            var rateLimiter = RateLimiter
                .WithRateLimit(updates)
                .Build();

            rateLimiter.State.Should().Be(RateLimiterState.PendingInitialization);
            
            updates.OnNext(new RateLimit(1, TimeSpan.FromSeconds(1)));

            rateLimiter.State.Should().Be(RateLimiterState.Ready);
            
            rateLimiter.Dispose();

            rateLimiter.State.Should().Be(RateLimiterState.Disposed);
        }

        [Fact]
        public void WaitUntilInitializedWithTimeoutWorksCorrectly()
        {
            var updates = new Subject<RateLimit>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(updates.Delay(TimeSpan.FromMilliseconds(500)))
                .Build();

            Action action = () => rateLimiter.CanExecute();

            action.Should().Throw<InvalidOperationException>();

            updates.OnNext(new RateLimit(1, TimeSpan.FromSeconds(1)));

            var timer = Stopwatch.StartNew();
            
            rateLimiter.WaitUntilInitialized(TimeSpan.FromMinutes(1));
            
            timer.Stop();

            timer.Elapsed.Should().BeCloseTo(TimeSpan.FromMilliseconds(500), 100);
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WaitUntilInitializedWithCancellationTokenWorksCorrectly(bool cancelBeforeComplete)
        {
            var updates = new Subject<RateLimit>();
            
            var rateLimiter = RateLimiter
                .WithRateLimit(updates.Delay(TimeSpan.FromMilliseconds(500)))
                .Build();

            Action action1 = () => rateLimiter.CanExecute();

            action1.Should().Throw<InvalidOperationException>();

            var cancellationTokenSource = cancelBeforeComplete
                ? new CancellationTokenSource(TimeSpan.FromMilliseconds(100))
                : new CancellationTokenSource();
            
            Action action2 = () => rateLimiter.WaitUntilInitialized(cancellationTokenSource.Token);

            updates.OnNext(new RateLimit(1, TimeSpan.FromSeconds(1)));

            var timer = Stopwatch.StartNew();

            if (cancelBeforeComplete)
            {
                action2.Should().ThrowExactly<OperationCanceledException>();
            }
            else
            {
                action2.Should().NotThrow();
                timer.Elapsed.Should().BeCloseTo(TimeSpan.FromMilliseconds(500), 100);
            }
        }
    }
}