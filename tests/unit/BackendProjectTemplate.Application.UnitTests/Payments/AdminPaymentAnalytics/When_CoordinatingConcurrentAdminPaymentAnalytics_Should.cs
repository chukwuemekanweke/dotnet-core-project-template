using BackendProjectTemplate.Application.Common.Concurrency;
using BackendProjectTemplate.Application.Payments.Analytics;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Collections.Concurrent;

namespace BackendProjectTemplate.Application.UnitTests.Payments.AdminPaymentAnalytics;

public sealed class When_CoordinatingConcurrentAdminPaymentAnalytics_Should
{
    [Fact]
    public async Task ExecuteOneRepositoryQueryForIdenticalConcurrentCacheMisses()
    {
        var repository = Substitute.For<IPaymentAnalyticsReadModelRepository>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;
        repository.GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref executions);
                entered.TrySetResult();
                await release.Task;
                return EmptySummary();
            });
        var service = CreateService(repository, new ControllableJsonCache());
        var query = Query(Guid.CreateVersion7());

        var requests = Enumerable.Range(0, 20)
            .Select(_ => service.GetSummaryAsync(query, CancellationToken.None))
            .ToArray();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        release.SetResult();
        await Task.WhenAll(requests);

        executions.ShouldBe(1);
    }

    [Fact]
    public async Task AllowDifferentCacheKeysToExecuteConcurrently()
    {
        var repository = Substitute.For<IPaymentAnalyticsReadModelRepository>();
        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;
        repository.GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                if (Interlocked.Increment(ref executions) == 2)
                    bothEntered.TrySetResult();
                await release.Task;
                return EmptySummary();
            });
        var service = CreateService(repository, new ControllableJsonCache());

        var first = service.GetSummaryAsync(Query(Guid.CreateVersion7()), CancellationToken.None);
        var second = service.GetSummaryAsync(Query(Guid.CreateVersion7()), CancellationToken.None);
        await bothEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        release.SetResult();
        await Task.WhenAll(first, second);

        executions.ShouldBe(2);
    }

    [Fact]
    public async Task FallBackOnCacheReadFailureAndIgnoreCacheWriteFailure()
    {
        var repository = Substitute.For<IPaymentAnalyticsReadModelRepository>();
        repository.GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(EmptySummary());
        var cache = new ControllableJsonCache { ThrowOnRead = true, ThrowOnWrite = true };
        var service = CreateService(repository, cache);

        var result = await service.GetSummaryAsync(Query(Guid.CreateVersion7()), CancellationToken.None);

        result.TransactionCount.ShouldBe(0);
        await repository.Received(1).GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>());
        cache.WriteCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReleaseSingleFlightEntryWhenRepositoryFails()
    {
        var repository = Substitute.For<IPaymentAnalyticsReadModelRepository>();
        repository.GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("PostgreSQL unavailable"), _ => EmptySummary());
        var service = CreateService(repository, new ControllableJsonCache());
        var query = Query(Guid.CreateVersion7());

        await Should.ThrowAsync<InvalidOperationException>(() => service.GetSummaryAsync(query, CancellationToken.None));
        var recovered = await service.GetSummaryAsync(query, CancellationToken.None);

        recovered.TransactionCount.ShouldBe(0);
        await repository.Received(2).GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelOnlyTheWaitingRequest()
    {
        var repository = Substitute.For<IPaymentAnalyticsReadModelRepository>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executions = 0;
        repository.GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                Interlocked.Increment(ref executions);
                entered.TrySetResult();
                await release.Task;
                return EmptySummary();
            });
        var cache = new ControllableJsonCache();
        var service = CreateService(repository, cache);
        var query = Query(Guid.CreateVersion7());
        var first = service.GetSummaryAsync(query, CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource();
        var waiting = service.GetSummaryAsync(query, cancellation.Token);
        await WaitUntilAsync(() => cache.ReadCount >= 3);

        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => waiting);
        release.SetResult();
        await first;
        var cached = await service.GetSummaryAsync(query, CancellationToken.None);

        cached.TransactionCount.ShouldBe(0);
        executions.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveIdleEntriesAfterRepeatedOperationsAndCancellation()
    {
        var keyedLock = new AsyncKeyedLock();
        for (var index = 0; index < 100; index++)
        {
            await using var lease = await keyedLock.AcquireAsync($"key-{index}", CancellationToken.None);
        }

        var held = await keyedLock.AcquireAsync("cancelled-waiter", CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var waiting = keyedLock.AcquireAsync("cancelled-waiter", cancellation.Token).AsTask();
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => waiting);
        await held.DisposeAsync();

        keyedLock.EntryCount.ShouldBe(0);
    }

    private static PaymentAnalyticsService CreateService(
        IPaymentAnalyticsReadModelRepository repository,
        IJsonCache cache) => new(
        repository,
        cache,
        new FixedTimeProvider(),
        Options.Create(new PaymentAnalyticsOptions()),
        Substitute.For<ILogger<PaymentAnalyticsService>>());

    private static PaymentAnalyticsQuery Query(Guid tenantId) => new(
        new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), null, null, null, null, tenantId);

    private static PaymentAnalyticsSummaryReadModel EmptySummary() => new(0, 0, 0, 0, 0, []);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class ControllableJsonCache : IJsonCache
    {
        private readonly ConcurrentDictionary<string, object> _values = new(StringComparer.Ordinal);
        private int _readCount;
        private int _writeCount;

        public bool ThrowOnRead { get; init; }
        public bool ThrowOnWrite { get; init; }
        public int ReadCount => Volatile.Read(ref _readCount);
        public int WriteCount => Volatile.Read(ref _writeCount);

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _readCount);
            if (ThrowOnRead)
                throw new InvalidOperationException("Redis read unavailable");
            return Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _writeCount);
            if (ThrowOnWrite)
                throw new InvalidOperationException("Redis write unavailable");
            _values[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _values.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public async Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            await RemoveAsync(key, cancellationToken);
            return value;
        }
    }
}
