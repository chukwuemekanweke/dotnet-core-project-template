using BackendProjectTemplate.Domain.Analytics.ReadModels;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed class GetRegistrationAnalyticsHandler(
    IRegistrationAnalyticsReadModelRepository repository,
    IJsonCache cache,
    TimeProvider timeProvider,
    IOptions<AdministrativeAnalyticsOptions> options,
    ILogger<GetRegistrationAnalyticsHandler> logger)
{
    private readonly AdministrativeAnalyticsOptions _options = options.Value;

    public async Task<RegistrationAnalyticsResponse> HandleAsync(
        RegistrationAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        var range = ResolveRange(query.From, query.To);
        var filter = new RegistrationAnalyticsFilter(
            StartOfDay(range.From), StartOfDay(range.To.AddDays(1)), query.TenantId);
        var cacheKey = BuildCacheKey(range.From, range.To, query.TenantId, query.Interval);
        var cached = await TryGetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var previousFrom = filter.FromUtc.AddDays(-range.DayCount);
        var counts = await repository.GetCountsAsync(filter, previousFrom, cancellationToken);
        var rows = await repository.GetTimelineAsync(filter, query.Interval, cancellationToken);
        decimal? percentageChange = counts.Previous == 0
            ? counts.Current == 0 ? 0m : null
            : decimal.Round((counts.Current - counts.Previous) * 100m / counts.Previous, 2);
        var response = new RegistrationAnalyticsResponse(
            range.From,
            range.To,
            "UTC",
            counts.AllTime,
            counts.Current,
            counts.Previous,
            percentageChange,
            FillTimeline(rows, range.From, range.To, query.Interval));
        await TrySetAsync(cacheKey, response, cancellationToken);
        return response;
    }

    private ReportingRange ResolveRange(DateOnly? fromValue, DateOnly? toValue)
    {
        var to = toValue ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = fromValue ?? to.AddDays(-(_options.DefaultRangeDays - 1));
        return new ReportingRange(from, to, to.DayNumber - from.DayNumber + 1);
    }

    private static DateTimeOffset StartOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

    private static IReadOnlyList<RegistrationAnalyticsTimelineItem> FillTimeline(
        IReadOnlyList<RegistrationAnalyticsTimelineRow> rows,
        DateOnly from,
        DateOnly to,
        Interval interval)
    {
        var lookup = rows.ToDictionary(row => row.PeriodStart, row => row.Count);
        var timeline = new List<RegistrationAnalyticsTimelineItem>();
        for (var period = AnalyticsCalendar.AlignPeriodStart(from, interval);
             period <= to;
             period = AnalyticsCalendar.AdvancePeriod(period, interval))
        {
            timeline.Add(new RegistrationAnalyticsTimelineItem(
                period < from ? from : period,
                AnalyticsCalendar.GetPeriodEnd(period, to, interval),
                lookup.GetValueOrDefault(period)));
        }

        return timeline;
    }

    private async Task<RegistrationAnalyticsResponse?> TryGetAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetAsync<RegistrationAnalyticsResponse>(key, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not read registration analytics cache entry {CacheKey}; querying PostgreSQL.", key);
            return null;
        }
    }

    private async Task TrySetAsync(
        string key,
        RegistrationAnalyticsResponse value,
        CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetAsync(key, value, _options.RegistrationTtl, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not write registration analytics cache entry {CacheKey}.", key);
        }
    }

    private static string BuildCacheKey(DateOnly from, DateOnly to, Guid? tenantId, Interval interval)
    {
        var canonical = string.Join('|',
            from.ToString("O", CultureInfo.InvariantCulture),
            to.ToString("O", CultureInfo.InvariantCulture),
            interval.ToString(),
            tenantId?.ToString("D") ?? "global",
            "UTC");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return $"users:analytics:v1:registrations:{hash}";
    }

    private sealed record ReportingRange(DateOnly From, DateOnly To, int DayCount);

}
