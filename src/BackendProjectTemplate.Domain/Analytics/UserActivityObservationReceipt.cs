using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Analytics;

public sealed class UserActivityObservationReceipt : Entity, IAggregateRoot
{
    private UserActivityObservationReceipt()
    {
    }

    private UserActivityObservationReceipt(
        Guid observationId,
        Guid tenantId,
        Guid stakeholderId,
        DateOnly activityDate,
        DateTimeOffset occurredAtUtc,
        string activityType)
    {
        ObservationId = observationId;
        TenantId = tenantId;
        StakeholderId = stakeholderId;
        ActivityDate = activityDate;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        ActivityType = activityType.Trim();
    }

    public Guid ObservationId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StakeholderId { get; private set; }
    public DateOnly ActivityDate { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string ActivityType { get; private set; } = string.Empty;

    public static UserActivityObservationReceipt Create(
        Guid observationId,
        Guid tenantId,
        Guid stakeholderId,
        DateOnly activityDate,
        DateTimeOffset occurredAtUtc,
        string activityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityType);
        return new(
            observationId,
            tenantId,
            stakeholderId,
            activityDate,
            occurredAtUtc,
            activityType);
    }
}
