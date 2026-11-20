using BackendProjectTemplate.Domain.Analytics.ReadModels;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Application.Analytics.Features;

public sealed class GetActiveUserAnalyticsHandler(
    IActiveUserAnalyticsReadModelRepository repository,
    IJsonCache cache,
    TimeProvider timeProvider,
    IOptions<AdministrativeAnalyticsOptions> options,
    ILogger<GetActiveUserAnalyticsHandler> logger)
{
    private readonly AdministrativeAnalyticsOptions _options = options.Value;

    public async Task<ActiveUserAnalyticsResponse> HandleAsync(
        ActiveUserAnalyticsQuery query,
        CancellationToken cancellationToken)
    {
        var range = ResolveRange(query.From, query.To);
        var cacheKey = BuildCacheKey(range.From, range.To, query.TenantId);
        var cached = await TryGetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var summary = await repository.GetSummaryAsync(range.To, query.TenantId, cancellationToken);
        var rows = await repository.GetTimelineAsync(range.From, range.To, query.TenantId, cancellationToken);
        var lookup = rows.ToDictionary(row => row.Date, row => row.Count);
        var timeline = Enumerable.Range(0, range.DayCount)
            .Select(offset => range.From.AddDays(offset))
            .Select(date => new ActiveUserAnalyticsTimelineItem(date, lookup.GetValueOrDefault(date)))
            .ToArray();
        var response = new ActiveUserAnalyticsResponse(
            range.From,
            range.To,
            "UTC",
            summary.Daily,
            summary.Weekly,
            summary.Monthly,
            AnalyticsMath.Percentage(summary.Daily, summary.Monthly),
            timeline);
        await TrySetAsync(cacheKey, response, cancellationToken);
        return response;
    }

    private ReportingRange ResolveRange(DateOnly? fromValue, DateOnly? toValue)
    {
        var to = toValue ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = fromValue ?? to.AddDays(-(_options.DefaultRangeDays - 1));
        return new ReportingRange(from, to, to.DayNumber - from.DayNumber + 1);
    }

    private async Task<ActiveUserAnalyticsResponse?> TryGetAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetAsync<ActiveUserAnalyticsResponse>(key, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not read active-user analytics cache entry {CacheKey}; querying PostgreSQL.", key);
            return null;
        }
    }

    private async Task TrySetAsync(
        string key,
        ActiveUserAnalyticsResponse value,
        CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetAsync(key, value, _options.ActiveUsersTtl, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not write active-user analytics cache entry {CacheKey}.", key);
        }
    }

    private static string BuildCacheKey(DateOnly from, DateOnly to, Guid? tenantId)
    {
        var canonical = string.Join('|',
            from.ToString("O", CultureInfo.InvariantCulture),
            to.ToString("O", CultureInfo.InvariantCulture),
            tenantId?.ToString("D") ?? "global",
            "UTC");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return $"users:analytics:v1:active-users:{hash}";
    }

    private sealed record ReportingRange(DateOnly From, DateOnly To, int DayCount);

}
