namespace BackendProjectTemplate.Domain.Analytics;

public interface IUserDailyActivityPersistence
{
    Task ObserveAsync(
        Guid observationId,
        Guid tenantId,
        Guid stakeholderId,
        DateOnly activityDate,
        DateTimeOffset occurredAtUtc,
        string activityType,
        CancellationToken cancellationToken);
}
