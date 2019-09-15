using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using FluentAssertions;
using NanoThrottle.Multi;
using Xunit;

namespace NanoThrottle.Tests.Multi
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

        [Fact]
        public void CanGetRateLimit()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, rateLimit)
            });

            rateLimiter.GetRateLimit(1).Should().Be(rateLimit);
        }
        
        [Fact]
        public void CanSetRateLimit()
        {
            var rateLimit = new RateLimit(1, TimeSpan.FromSeconds(1));
            
            var rateLimiter = new RateLimiter<int>(new[]
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
            var rateLimiter = new RateLimiter<int>(new[]
            {    
                new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1)))
            }, instanceCount);

            rateLimiter.InstanceCount.Should().Be(instanceCount);
        }
        
        [Fact]
        public void CanSetInstanceCount()
        {
            var rateLimiter = new RateLimiter<int>(new[]
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
            
            var rateLimiter = new RateLimiter<int>(new[]
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
            
            var rateLimiter = new RateLimiter<int>(new[]
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
            
            var rateLimiter = new RateLimiter<int>(new[]
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
        
        [Fact]
        public void OnInstanceCountChangedIsTriggeredCorrectly()
        {
            var instanceCountChanges = new List<InstanceCountChangedNotification>();

            Action<InstanceCountChangedNotification> onInstanceCountChanged = instanceCountChanges.Add;

            var rateLimiter = new RateLimiter<int>(new[]
            {
                new KeyValuePair<int, RateLimit>(1, new RateLimit(10, TimeSpan.FromSeconds(1)))
            }, onInstanceCountChanged: onInstanceCountChanged);
            
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

        [Fact]
        public void WithRateLimitUpdatesWorksCorrectly()
        {
            var updates = new Subject<IEnumerable<KeyValuePair<int, RateLimit>>>();

            var rateLimiter = RateLimiter
                .WithRateLimits(updates)
                .Build();

            for (var i = 0; i < 10; i++)
            {
                var rateLimit = new RateLimit(i, TimeSpan.FromSeconds(1));

                updates.OnNext(new[] { new KeyValuePair<int, RateLimit>(1, rateLimit) });

                rateLimiter.GetRateLimit(1).Should().Be(rateLimit);
            }
        }

        [Fact]
        public void WithInstanceCountUpdatesWorksCorrectly()
        {
            var updates = new Subject<int>();

            var rateLimiter = RateLimiter
                .WithRateLimits(new[]
                {
                    new KeyValuePair<int, RateLimit>(1, new RateLimit(100, TimeSpan.FromSeconds(1)))
                })
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
            var updates = new Subject<IEnumerable<KeyValuePair<int, RateLimit>>>();

            var rateLimiter = RateLimiter
                .WithRateLimits(updates)
                .Build();

            Action action = () => rateLimiter.CanExecute(1);

            action.Should().Throw<InvalidOperationException>();
        }
        
        [Fact]
        public void ThrowsIfDisposed()
        {
            var updates = new Subject<IEnumerable<KeyValuePair<int, RateLimit>>>();

            var rateLimiter = RateLimiter
                .WithRateLimits(updates)
                .Build();

            rateLimiter.Dispose();
            
            Action action = () => rateLimiter.CanExecute(1);

            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void SubscribesToUpdatesImmediately()
        {
            var rateLimitUpdates = new Subject<IEnumerable<KeyValuePair<int, RateLimit>>>();
            var instanceCountUpdates = new Subject<int>();
            
            var rateLimiter = RateLimiter
                .WithRateLimits(rateLimitUpdates)
                .WithInstanceCount(instanceCountUpdates)
                .Build();

            rateLimitUpdates.HasObservers.Should().BeTrue();
            instanceCountUpdates.HasObservers.Should().BeTrue();
        }
        
        [Fact]
        public void UnsubscribesFromUpdatesOnDispose()
        {
            var rateLimitUpdates = new Subject<IEnumerable<KeyValuePair<int, RateLimit>>>();
            var instanceCountUpdates = new Subject<int>();
            
            var rateLimiter = RateLimiter
                .WithRateLimits(rateLimitUpdates)
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
            var updates = new Subject<IEnumerable<KeyValuePair<int, RateLimit>>>();

            var rateLimiter = RateLimiter
                .WithRateLimits(updates)
                .Build();

            rateLimiter.State.Should().Be(RateLimiterState.PendingInitialization);
            
            updates.OnNext(new[] { new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1))) });

            rateLimiter.State.Should().Be(RateLimiterState.Ready);
            
            rateLimiter.Dispose();

            rateLimiter.State.Should().Be(RateLimiterState.Disposed);
        }
        
        [Fact]
        public void WaitUntilInitializedWithTimeoutWorksCorrectly()
        {
            var updates = new Subject<IEnumerable<KeyValuePair<int, RateLimit>>>();
            
            var rateLimiter = RateLimiter
                .WithRateLimits(updates.Delay(TimeSpan.FromMilliseconds(500)))
                .Build();

            Action action = () => rateLimiter.CanExecute(1);

            action.Should().Throw<InvalidOperationException>();

            updates.OnNext(new[] { new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1))) });

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
            var updates = new Subject<IEnumerable<KeyValuePair<int, RateLimit>>>();
            
            var rateLimiter = RateLimiter
                .WithRateLimits(updates.Delay(TimeSpan.FromMilliseconds(500)))
                .Build();

            Action action1 = () => rateLimiter.CanExecute(1);
            
            action1.Should().Throw<InvalidOperationException>();

            var cancellationTokenSource = cancelBeforeComplete
                ? new CancellationTokenSource(TimeSpan.FromMilliseconds(100))
                : new CancellationTokenSource();
            
            Action action2 = () => rateLimiter.WaitUntilInitialized(cancellationTokenSource.Token);

            updates.OnNext(new[] { new KeyValuePair<int, RateLimit>(1, new RateLimit(1, TimeSpan.FromSeconds(1))) });

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