using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Analytics;

public sealed class UserDailyActivity : Entity, IAggregateRoot
{
    private UserDailyActivity()
    {
    }

    private UserDailyActivity(
        Guid tenantId,
        Guid stakeholderId,
        DateOnly activityDate,
        DateTimeOffset occurredAtUtc)
    {
        TenantId = tenantId;
        StakeholderId = stakeholderId;
        ActivityDate = activityDate;
        FirstActivityAtUtc = occurredAtUtc.ToUniversalTime();
        LastActivityAtUtc = occurredAtUtc.ToUniversalTime();
        ActivityCount = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid StakeholderId { get; private set; }
    public DateOnly ActivityDate { get; private set; }
    public DateTimeOffset FirstActivityAtUtc { get; private set; }
    public DateTimeOffset LastActivityAtUtc { get; private set; }
    public long ActivityCount { get; private set; }

    public static UserDailyActivity Create(
        Guid tenantId,
        Guid stakeholderId,
        DateOnly activityDate,
        DateTimeOffset occurredAtUtc) =>
        new(tenantId, stakeholderId, activityDate, occurredAtUtc);

    public void Observe(DateTimeOffset occurredAtUtc)
    {
        occurredAtUtc = occurredAtUtc.ToUniversalTime();
        if (occurredAtUtc < FirstActivityAtUtc)
        {
            FirstActivityAtUtc = occurredAtUtc;
        }

        if (occurredAtUtc > LastActivityAtUtc)
        {
            LastActivityAtUtc = occurredAtUtc;
        }

        ActivityCount++;
    }
}
