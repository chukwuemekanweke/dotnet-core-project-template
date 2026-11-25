using BackendProjectTemplate.Application.Subscriptions.Analytics;
using BackendProjectTemplate.Domain.Common.Caching;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AdminSubscriptionAnalytics;

internal static class SubscriptionAnalyticsHandlerTestContext
{
    internal static SubscriptionAnalyticsCache CreateCache() => new(
        new MemoryJsonCache(),
        Substitute.For<ILogger<SubscriptionAnalyticsCache>>());

    internal static TimeProvider CreateTimeProvider() =>
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
