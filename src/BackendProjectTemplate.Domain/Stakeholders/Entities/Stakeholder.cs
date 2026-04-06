using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Entities;

public sealed class Stakeholder : Entity
{
    private Stakeholder()
    {
    }

    private Stakeholder(
        Guid tenantId,
        Guid countryId,
        Guid stakeholderTypeId,
        DateTimeOffset utcNow)
    {
        TenantId = tenantId;
        CountryId = countryId;
        StakeholderTypeId = stakeholderTypeId;
        SetAuditDates(utcNow);
    }

    public Guid TenantId { get; private set; }
    public Guid CountryId { get; private set; }
    public Guid StakeholderTypeId { get; private set; }
    public StakeholderType StakeholderType { get; private set; } = null!;

    public ICollection<AppUserStakeholder> AppUserStakeholders { get; private set; } = [];

    public static Stakeholder Create(
        Guid tenantId,
        Guid countryId,
        Guid stakeholderTypeId,
        DateTimeOffset utcNow) =>
        new(tenantId, countryId, stakeholderTypeId, utcNow);
}
