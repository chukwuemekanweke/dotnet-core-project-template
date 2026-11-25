using BackendProjectTemplate.Application.Common.Concurrency;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Caching;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BackendProjectTemplate.Application.Subscriptions.Analytics;

public sealed class SubscriptionAnalyticsCache(
    IJsonCache cache,
    ILogger<SubscriptionAnalyticsCache> logger)
{
    private static readonly AsyncKeyedLock SingleFlight = new();

    public async Task<T> GetOrCreateAsync<T>(
        string endpoint,
        SubscriptionAnalyticsFilter filter,
        Interval? interval,
        TimeSpan ttl,
        Func<Task<T>> factory,
        CancellationToken cancellationToken) where T : class
    {
        var key = BuildKey(endpoint, filter, interval);
        var cached = await TryGetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        await using var lease = await SingleFlight.AcquireAsync(key, cancellationToken);
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
            logger.LogWarning(exception, "Could not write subscription analytics cache entry {CacheKey}.", key);
        }

        return value;
    }

    private async Task<T?> TryGetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        try
        {
            return await cache.GetAsync<T>(key, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not read subscription analytics cache entry {CacheKey}; querying PostgreSQL.", key);
            return null;
        }
    }

    private static string BuildKey(
        string endpoint,
        SubscriptionAnalyticsFilter filter,
        Interval? interval)
    {
        var canonical = string.Join('|',
            filter.FromUtc.ToString("O", CultureInfo.InvariantCulture),
            filter.ToExclusiveUtc.ToString("O", CultureInfo.InvariantCulture),
            interval?.ToString() ?? "-",
            filter.PlanId?.ToString("D") ?? "-",
            filter.OwnerType?.ToString() ?? "-",
            filter.TenantId?.ToString("D") ?? "global",
            filter.CurrencyId?.ToString("D") ?? "-",
            "UTC");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return $"subscriptions:analytics:v1:{endpoint}:{hash}";
    }
}
