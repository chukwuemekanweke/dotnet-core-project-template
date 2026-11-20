namespace BackendProjectTemplate.Domain.Analytics;

public interface IUserActivityThrottle
{
    Task<bool> TryClaimAsync(
        string key,
        Guid observationId,
        TimeSpan ttl,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        string key,
        Guid observationId,
        CancellationToken cancellationToken);
}
