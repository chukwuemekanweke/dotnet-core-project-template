using BackendProjectTemplate.Domain.Analytics;
using StackExchange.Redis;

namespace BackendProjectTemplate.Infrastructure.Analytics;

internal sealed class RedisUserActivityThrottle(IConnectionMultiplexer connectionMultiplexer) : IUserActivityThrottle
{
    public Task<bool> TryClaimAsync(
        string key,
        Guid observationId,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return connectionMultiplexer.GetDatabase().LockTakeAsync(
            key,
            observationId.ToString("N"),
            ttl);
    }

    public async Task ReleaseAsync(
        string key,
        Guid observationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await connectionMultiplexer.GetDatabase().LockReleaseAsync(
            key,
            observationId.ToString("N"));
    }
}
