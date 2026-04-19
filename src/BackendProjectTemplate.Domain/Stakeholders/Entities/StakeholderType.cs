using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Entities;

public sealed class StakeholderType : Entity, IAggregateRoot
{
    private StakeholderType()
    {
    }

    private StakeholderType(Guid tenantId, string name, string key, DateTimeOffset utcNow)
    {
        TenantId = tenantId;
        Name = name.Trim();
        Key = key.Trim();
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;

    public ICollection<Stakeholder> Stakeholders { get; private set; } = [];

    public static StakeholderType Create(Guid tenantId, string name, string key, DateTimeOffset utcNow) =>
        new(tenantId, name, key, utcNow);
}
