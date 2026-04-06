using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Entities;

public sealed class StakeholderType : Entity
{
    private StakeholderType()
    {
    }

    private StakeholderType(string name, string key, DateTimeOffset utcNow)
    {
        Name = name.Trim();
        Key = key.Trim();
        SetAuditDates(utcNow);
    }

    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;

    public ICollection<Stakeholder> Stakeholders { get; private set; } = [];

    public static StakeholderType Create(string name, string key, DateTimeOffset utcNow) =>
        new(name, key, utcNow);
}
