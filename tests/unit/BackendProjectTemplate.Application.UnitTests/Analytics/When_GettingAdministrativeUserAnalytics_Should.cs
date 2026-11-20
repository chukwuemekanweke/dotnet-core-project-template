using BackendProjectTemplate.Application.Analytics;
using BackendProjectTemplate.Application.Analytics.Features;
using BackendProjectTemplate.Domain.Analytics.ReadModels;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Collections.Concurrent;

namespace BackendProjectTemplate.Application.UnitTests.Analytics;

public sealed class When_GettingAdministrativeUserAnalytics_Should
{
    [Fact]
    public async Task CalculateComparableRegistrationPeriodAndFillMondayAlignedBuckets()
    {
        var registrations = Substitute.For<IRegistrationAnalyticsReadModelRepository>();
        registrations.GetCountsAsync(
                Arg.Any<RegistrationAnalyticsFilter>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationAnalyticsCounts(25, 5, 0));
        registrations.GetTimelineAsync(
                Arg.Any<RegistrationAnalyticsFilter>(),
                Interval.Week,
                Arg.Any<CancellationToken>())
            .Returns([
                new RegistrationAnalyticsTimelineRow(new DateOnly(2026, 10, 26), 2),
                new RegistrationAnalyticsTimelineRow(new DateOnly(2026, 11, 2), 3)
            ]);
        var handler = CreateRegistrationHandler(registrations);

        var response = await handler.HandleAsync(
            new RegistrationAnalyticsQuery(
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 11, 10),
                Interval.Week,
                null),
            CancellationToken.None);

        response.AllTimeCount.ShouldBe(25);
        response.RegistrationCount.ShouldBe(5);
        response.PercentageChange.ShouldBeNull();
        response.Timeline.Count.ShouldBe(3);
        response.Timeline[0].From.ShouldBe(new DateOnly(2026, 11, 1));
        response.Timeline[0].To.ShouldBe(new DateOnly(2026, 11, 1));
        response.Timeline[2].Count.ShouldBe(0);
        await registrations.Received(1).GetCountsAsync(
            Arg.Is<RegistrationAnalyticsFilter>(filter =>
                filter.FromUtc == new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero) &&
                filter.ToExclusiveUtc == new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero)),
            new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnchorRollingActiveMetricsToEndDateAndFillMissingDays()
    {
        var activeUsers = Substitute.For<IActiveUserAnalyticsReadModelRepository>();
        activeUsers.GetSummaryAsync(new DateOnly(2026, 11, 20), null, Arg.Any<CancellationToken>())
            .Returns(new ActiveUserAnalyticsSummary(2, 3, 4));
        activeUsers.GetTimelineAsync(
                new DateOnly(2026, 11, 18),
                new DateOnly(2026, 11, 20),
                null,
                Arg.Any<CancellationToken>())
            .Returns([
                new ActiveUserAnalyticsDailyRow(new DateOnly(2026, 11, 18), 1),
                new ActiveUserAnalyticsDailyRow(new DateOnly(2026, 11, 20), 2)
            ]);
        var handler = CreateActiveUserHandler(activeUsers);

        var response = await handler.HandleAsync(
            new ActiveUserAnalyticsQuery(
                new DateOnly(2026, 11, 18),
                new DateOnly(2026, 11, 20),
                null),
            CancellationToken.None);

        response.DailyActiveUsers.ShouldBe(2);
        response.WeeklyActiveUsers.ShouldBe(3);
        response.MonthlyActiveUsers.ShouldBe(4);
        response.DailyToMonthlyPercentage.ShouldBe(50m);
        response.Timeline.Select(item => item.Count).ShouldBe([1, 0, 2]);
    }

    [Fact]
    public async Task UseThirtyCalendarDayDefaultRangeAndCacheByTenant()
    {
        var registrations = Substitute.For<IRegistrationAnalyticsReadModelRepository>();
        registrations.GetCountsAsync(
                Arg.Any<RegistrationAnalyticsFilter>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationAnalyticsCounts(0, 0, 0));
        registrations.GetTimelineAsync(
                Arg.Any<RegistrationAnalyticsFilter>(),
                Interval.Day,
                Arg.Any<CancellationToken>())
            .Returns([]);
        var handler = CreateRegistrationHandler(registrations);

        var firstTenant = Guid.CreateVersion7();
        await handler.HandleAsync(
            new RegistrationAnalyticsQuery(null, null, Interval.Day, firstTenant),
            CancellationToken.None);
        await handler.HandleAsync(
            new RegistrationAnalyticsQuery(null, null, Interval.Day, firstTenant),
            CancellationToken.None);
        await handler.HandleAsync(
            new RegistrationAnalyticsQuery(null, null, Interval.Day, Guid.CreateVersion7()),
            CancellationToken.None);

        await registrations.Received(2).GetCountsAsync(
            Arg.Is<RegistrationAnalyticsFilter>(filter =>
                filter.FromUtc == new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero) &&
                filter.ToExclusiveUtc == new DateTimeOffset(2026, 11, 21, 0, 0, 0, TimeSpan.Zero)),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    private static GetRegistrationAnalyticsHandler CreateRegistrationHandler(
        IRegistrationAnalyticsReadModelRepository repository) =>
        new(
            repository,
            new MemoryJsonCache(),
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)),
            Options.Create(new AdministrativeAnalyticsOptions()),
            Substitute.For<ILogger<GetRegistrationAnalyticsHandler>>());

    private static GetActiveUserAnalyticsHandler CreateActiveUserHandler(
        IActiveUserAnalyticsReadModelRepository repository) =>
        new(
            repository,
            new MemoryJsonCache(),
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)),
            Options.Create(new AdministrativeAnalyticsOptions()),
            Substitute.For<ILogger<GetActiveUserAnalyticsHandler>>());

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
