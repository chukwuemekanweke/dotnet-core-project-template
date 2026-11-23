using BackendProjectTemplate.Application.Payments.Analytics;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Collections.Concurrent;

namespace BackendProjectTemplate.Application.UnitTests.Payments.AdminPaymentAnalytics;

public sealed class When_GettingAdminPaymentAnalytics_Should
{
    [Fact]
    public async Task CalculateTerminalMetricsKeepCurrenciesSeparateAndCacheEquivalentRequests()
    {
        var repository = Substitute.For<IPaymentAnalyticsReadModelRepository>();
        var ngnId = Guid.CreateVersion7();
        var usdId = Guid.CreateVersion7();
        repository.GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentAnalyticsSummaryReadModel(
                8,
                3,
                3,
                1,
                2,
                [
                    new(ngnId, "NGN", 6, 2, 1, 1_000m, 600m, 100m, 100m, 100m, 100m, 40m),
                    new(usdId, "USD", 2, 1, 0, 50m, 30m, 20m, 0m, 0m, 0m, 0m)
                ]));
        var cache = new MemoryJsonCache();
        var service = CreateService(repository, cache);
        var query = new PaymentAnalyticsQuery(
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), null, null, null, null, null);

        var first = await service.GetSummaryAsync(query, CancellationToken.None);
        var second = await service.GetSummaryAsync(query, CancellationToken.None);

        first.SuccessRate.ShouldBe(50m);
        first.UnderpaidCount.ShouldBe(1);
        first.Currencies.Count.ShouldBe(2);
        first.Currencies.Single(item => item.CurrencyCode == "NGN").UnderpaidVolume.ShouldBe(40m);
        first.Currencies.Single(item => item.CurrencyCode == "NGN").AverageSuccessfulTransactionValue.ShouldBe(300m);
        first.Currencies.Single(item => item.CurrencyCode == "USD").AttemptedVolume.ShouldBe(50m);
        second.ShouldBe(first);
        await repository.Received(1).GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallBackToRepositoryWhenCacheIsUnavailable()
    {
        var repository = Substitute.For<IPaymentAnalyticsReadModelRepository>();
        repository.GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentAnalyticsSummaryReadModel(0, 0, 0, 0, 0, []));
        var service = CreateService(repository, new FailingJsonCache());

        var result = await service.GetSummaryAsync(
            new PaymentAnalyticsQuery(null, null, null, null, null, null, Guid.CreateVersion7()),
            CancellationToken.None);

        result.TransactionCount.ShouldBe(0);
        result.SuccessRate.ShouldBe(0m);
        await repository.Received(1).GetSummaryAsync(Arg.Any<PaymentAnalyticsFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FillMissingMondayAlignedTimelineBucketsPerCurrency()
    {
        var repository = Substitute.For<IPaymentAnalyticsReadModelRepository>();
        var currencyId = Guid.CreateVersion7();
        repository.GetTimelineAsync(Arg.Any<PaymentAnalyticsFilter>(), Interval.Week, Arg.Any<CancellationToken>())
            .Returns([
                new PaymentAnalyticsTimelineRow(
                    new DateOnly(2026, 11, 2), currencyId, "NGN", 1, 1, 0, 0, 0, 10m, 10m, 0m, 0m,
                    0m, 0m, 0m)
            ]);
        var service = CreateService(repository, new MemoryJsonCache());

        var result = await service.GetTimelineAsync(
            new PaymentAnalyticsQuery(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 15), null, null, null, null, null),
            Interval.Week,
            CancellationToken.None);

        result.Buckets.Count.ShouldBe(2);
        result.Buckets[0].PeriodStart.ShouldBe(new DateOnly(2026, 11, 2));
        result.Buckets[1].PeriodStart.ShouldBe(new DateOnly(2026, 11, 9));
        result.Buckets[1].TransactionCount.ShouldBe(0);
    }

    private static PaymentAnalyticsService CreateService(
        IPaymentAnalyticsReadModelRepository repository,
        IJsonCache cache) => new(
        repository,
        cache,
        new FixedTimeProvider(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero)),
        Options.Create(new PaymentAnalyticsOptions()),
        Substitute.For<ILogger<PaymentAnalyticsService>>());

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class MemoryJsonCache : IJsonCache
    {
        private readonly ConcurrentDictionary<string, object> _values = new(StringComparer.Ordinal);
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) =>
            Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
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

    private sealed class FailingJsonCache : IJsonCache
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) => throw new InvalidOperationException("Redis unavailable");
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) => throw new InvalidOperationException("Redis unavailable");
        public Task RemoveAsync(string key, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
