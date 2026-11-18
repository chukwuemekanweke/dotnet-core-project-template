using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Application.Payments.Analytics;

public sealed class PaymentAnalyticsService(
    IPaymentAnalyticsReadModelRepository repository,
    IJsonCache cache,
    TimeProvider timeProvider,
    IOptions<PaymentAnalyticsOptions> options,
    ILogger<PaymentAnalyticsService> logger)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);
    private readonly PaymentAnalyticsOptions _options = options.Value;

    public Task<PaymentAnalyticsSummaryResponse> GetSummaryAsync(PaymentAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var (filter, _, _) = Resolve(query);
        return GetOrCreateAsync("summary", filter, null, _options.SummaryTtl, async () =>
        {
            var result = await repository.GetSummaryAsync(filter, cancellationToken);
            return new PaymentAnalyticsSummaryResponse(
                result.TransactionCount,
                result.SuccessfulCount,
                result.UnsuccessfulCount,
                result.ProcessingCount,
                AnalyticsMath.Percentage(result.SuccessfulCount, result.SuccessfulCount + result.UnsuccessfulCount),
                result.Currencies.Select(item => new PaymentAnalyticsCurrencyResponse(
                    item.CurrencyId, item.CurrencyCode, item.TransactionCount, item.AttemptedVolume,
                    item.SuccessfulVolume, item.FailedVolume, item.ProcessingVolume, item.CancelledVolume,
                    item.ExpiredVolume, AnalyticsMath.Average(item.SuccessfulVolume, item.SuccessfulCount))).ToArray());
        }, cancellationToken);
    }

    public Task<PaymentAnalyticsTimelineResponse> GetTimelineAsync(
        PaymentAnalyticsQuery query,
        Interval interval,
        CancellationToken cancellationToken)
    {
        var (filter, from, to) = Resolve(query);
        return GetOrCreateAsync("timeline", filter, interval, _options.TimelineTtl, async () =>
        {
            var rows = await repository.GetTimelineAsync(filter, interval, cancellationToken);
            return new PaymentAnalyticsTimelineResponse(interval.ToString(), FillTimeline(rows, from, to, interval));
        }, cancellationToken);
    }

    public Task<PaymentAnalyticsStatusesResponse> GetStatusesAsync(PaymentAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var (filter, _, _) = Resolve(query);
        return GetOrCreateAsync("statuses", filter, null, _options.StatusesTtl, async () =>
        {
            var rows = await repository.GetStatusesAsync(filter, cancellationToken);
            var totals = rows.GroupBy(item => item.CurrencyId).ToDictionary(group => group.Key, group => group.Sum(item => item.TransactionCount));
            return new PaymentAnalyticsStatusesResponse(rows.Select(item => new PaymentAnalyticsStatusResponse(
                item.PaymentStatus.ToString(), item.CurrencyId, item.CurrencyCode, item.TransactionCount,
                item.TotalAmount, AnalyticsMath.Percentage(item.TransactionCount, totals[item.CurrencyId]))).ToArray());
        }, cancellationToken);
    }

    public Task<PaymentAnalyticsProvidersResponse> GetProvidersAsync(PaymentAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var (filter, _, _) = Resolve(query);
        return GetOrCreateAsync("providers", filter, null, _options.ProvidersTtl, async () =>
        {
            var rows = await repository.GetProvidersAsync(filter, cancellationToken);
            return new PaymentAnalyticsProvidersResponse(rows.Select(item => new PaymentAnalyticsProviderResponse(
                item.PaymentProviderId, item.PaymentProviderName, item.PaymentProviderKey, item.CurrencyId,
                item.CurrencyCode, item.TransactionCount, item.SuccessfulCount, item.UnsuccessfulCount,
                item.ProcessingCount, item.AttemptedVolume, item.SuccessfulVolume, item.FailedVolume,
                item.ProcessingVolume, item.CancelledVolume, item.ExpiredVolume,
                AnalyticsMath.Percentage(item.SuccessfulCount, item.SuccessfulCount + item.UnsuccessfulCount),
                AnalyticsMath.Average(item.SuccessfulVolume, item.SuccessfulCount))).ToArray());
        }, cancellationToken);
    }

    public Task<PaymentAnalyticsIntentsResponse> GetIntentsAsync(PaymentAnalyticsQuery query, CancellationToken cancellationToken)
    {
        var (filter, _, _) = Resolve(query);
        return GetOrCreateAsync("intents", filter, null, _options.IntentsTtl, async () =>
        {
            var rows = await repository.GetIntentsAsync(filter, cancellationToken);
            return new PaymentAnalyticsIntentsResponse(rows.Select(item => new PaymentAnalyticsIntentResponse(
                item.PaymentIntent.ToString(), item.CurrencyId, item.CurrencyCode, item.TransactionCount,
                item.SuccessfulCount, item.UnsuccessfulCount, item.ProcessingCount, item.AttemptedVolume,
                item.SuccessfulVolume, item.FailedVolume, item.ProcessingVolume, item.CancelledVolume,
                item.ExpiredVolume, AnalyticsMath.Percentage(item.SuccessfulCount, item.SuccessfulCount + item.UnsuccessfulCount),
                AnalyticsMath.Average(item.SuccessfulVolume, item.SuccessfulCount))).ToArray());
        }, cancellationToken);
    }

    private (PaymentAnalyticsFilter Filter, DateOnly From, DateOnly To) Resolve(PaymentAnalyticsQuery query)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var to = query.To ?? today;
        var from = query.From ?? to.AddDays(-(_options.DefaultRangeDays - 1));
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var toExclusiveUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        return (new PaymentAnalyticsFilter(
            fromUtc, toExclusiveUtc, query.PaymentProviderId, query.CurrencyId,
            query.PaymentIntent, query.PaymentStatus, query.TenantId), from, to);
    }

    private async Task<T> GetOrCreateAsync<T>(
        string endpoint,
        PaymentAnalyticsFilter filter,
        Interval? interval,
        TimeSpan ttl,
        Func<Task<T>> factory,
        CancellationToken cancellationToken) where T : class
    {
        var key = BuildCacheKey(endpoint, filter, interval);
        var cached = await TryGetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        var gate = Locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            cached = await TryGetAsync<T>(key, cancellationToken);
            if (cached is not null)
                return cached;
            var value = await factory();
            try
            {
                await cache.SetAsync(key, value, ttl, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not write payment analytics cache entry {CacheKey}.", key);
            }
            return value;
        }
        finally
        {
            gate.Release();
            Locks.TryRemove(new KeyValuePair<string, SemaphoreSlim>(key, gate));
        }
    }

    private async Task<T?> TryGetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        try
        {
            return await cache.GetAsync<T>(key, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not read payment analytics cache entry {CacheKey}; querying PostgreSQL.", key);
            return null;
        }
    }

    private static string BuildCacheKey(string endpoint, PaymentAnalyticsFilter filter, Interval? interval)
    {
        var canonical = string.Join('|',
            filter.FromUtc.ToString("O", CultureInfo.InvariantCulture),
            filter.ToExclusiveUtc.ToString("O", CultureInfo.InvariantCulture),
            interval?.ToString() ?? "-",
            filter.PaymentProviderId?.ToString("D") ?? "-",
            filter.CurrencyId?.ToString("D") ?? "-",
            filter.PaymentIntent?.ToString() ?? "-",
            filter.PaymentStatus?.ToString() ?? "-",
            filter.TenantId?.ToString("D") ?? "global",
            "UTC");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return $"payments:analytics:v1:{endpoint}:{hash}";
    }

    private static IReadOnlyList<PaymentAnalyticsTimelineBucketResponse> FillTimeline(
        IReadOnlyList<PaymentAnalyticsTimelineRow> rows,
        DateOnly from,
        DateOnly to,
        Interval interval)
    {
        var currencies = rows.Select(item => new { item.CurrencyId, item.CurrencyCode }).Distinct().ToArray();
        if (currencies.Length == 0)
            return [];
        var lookup = rows.ToDictionary(item => (item.PeriodStart, item.CurrencyId));
        var buckets = new List<PaymentAnalyticsTimelineBucketResponse>();
        for (var period = AnalyticsCalendar.AlignPeriodStart(from, interval);
             period <= to;
             period = AnalyticsCalendar.AdvancePeriod(period, interval))
        {
            var periodEnd = AnalyticsCalendar.GetPeriodEnd(period, to, interval);
            foreach (var currency in currencies)
            {
                if (lookup.TryGetValue((period, currency.CurrencyId), out var row))
                {
                    buckets.Add(new PaymentAnalyticsTimelineBucketResponse(
                        period, periodEnd, row.CurrencyId, row.CurrencyCode, row.TransactionCount,
                        row.SuccessfulCount, row.UnsuccessfulCount, row.ProcessingCount, row.AttemptedVolume,
                        row.SuccessfulVolume, row.FailedVolume, row.ProcessingVolume, row.CancelledVolume, row.ExpiredVolume));
                }
                else
                {
                    buckets.Add(new PaymentAnalyticsTimelineBucketResponse(
                        period, periodEnd, currency.CurrencyId, currency.CurrencyCode, 0, 0, 0, 0, 0m, 0m, 0m, 0m, 0m, 0m));
                }
            }
        }
        return buckets;
    }

}
