using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsRenewals;
using BackendProjectTemplate.Application.Subscriptions.Features.GetAdminSubscriptionAnalyticsSummary;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Collections.Concurrent;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AdminSubscriptionAnalytics;

public sealed class When_GettingAdminSubscriptionAnalytics_Should
{
    [Fact]
    public async Task CalculateTerminalRenewalRateKeepCurrenciesSeparateAndCacheEquivalentRequests()
    {
        var repository = Substitute.For<ISubscriptionAnalyticsReadModelRepository>();
        var ngnId = Guid.CreateVersion7();
        var usdId = Guid.CreateVersion7();
        repository.GetSummaryAsync(Arg.Any<SubscriptionAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(new SubscriptionAnalyticsSummaryReadModel(
                4,
                2,
                1,
                1,
                1,
                3,
                1,
                [new(ngnId, "NGN", 1_000m), new(usdId, "USD", 25m)]));
        var handler = CreateSummaryHandler(repository);
        var query = new SubscriptionAnalyticsQuery(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            null,
            null,
            null,
            null);

        var first = await handler.HandleAsync(query, CancellationToken.None);
        var second = await handler.HandleAsync(query, CancellationToken.None);

        first.RenewalSuccessRate.ShouldBe(75m);
        first.Revenues.Count.ShouldBe(2);
        first.Revenues.Single(item => item.CurrencyId == ngnId).Arr.ShouldBe(12_000m);
        first.Revenues.Single(item => item.CurrencyId == usdId).Mrr.ShouldBe(25m);
        first.SnapshotAtUtc.ShouldBe(new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(-1));
        second.ShouldBe(first);
        await repository.Received(1).GetSummaryAsync(
            Arg.Is<SubscriptionAnalyticsFilter>(filter =>
                filter.FromUtc == new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero) &&
                filter.ToExclusiveUtc == new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnZeroRateWhenThereAreNoTerminalRenewals()
    {
        var repository = Substitute.For<ISubscriptionAnalyticsReadModelRepository>();
        repository.GetRenewalsAsync(Arg.Any<SubscriptionAnalyticsFilter>(), Arg.Any<CancellationToken>())
            .Returns(new SubscriptionAnalyticsRenewalsReadModel(0, 0, 2, 3, 1));
        var handler = CreateRenewalsHandler(repository);

        var result = await handler.HandleAsync(
            new SubscriptionAnalyticsQuery(null, null, null, null, null, null),
            CancellationToken.None);

        result.SuccessRate.ShouldBe(0m);
        result.FailedPaymentAttempts.ShouldBe(2);
        result.PendingRenewals.ShouldBe(3);
    }

    private static GetAdminSubscriptionAnalyticsSummaryHandler CreateSummaryHandler(
        ISubscriptionAnalyticsReadModelRepository repository) => new(
        repository,
        CreateCache(),
        CreateTimeProvider(),
        Options.Create(new SubscriptionAnalyticsOptions()));

    private static GetAdminSubscriptionAnalyticsRenewalsHandler CreateRenewalsHandler(
        ISubscriptionAnalyticsReadModelRepository repository) => new(
        repository,
        CreateCache(),
        CreateTimeProvider(),
        Options.Create(new SubscriptionAnalyticsOptions()));

    private static SubscriptionAnalyticsCache CreateCache() => new(
        new MemoryJsonCache(),
        Substitute.For<ILogger<SubscriptionAnalyticsCache>>());

    private static TimeProvider CreateTimeProvider() =>
        new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));

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
}
