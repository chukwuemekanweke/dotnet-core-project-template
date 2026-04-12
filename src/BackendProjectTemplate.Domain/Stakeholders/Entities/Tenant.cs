using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Entities;

public sealed class Tenant : Entity
{
    private Tenant()
    {
    }

    private Tenant(
        Guid id,
        string name,
        string brandKey,
        DateTimeOffset utcNow)
    {
        Id = id;
        Name = name.Trim();
        BrandKey = brandKey.Trim().ToLowerInvariant();
        SetAuditDates(utcNow);
    }

    public string Name { get; private set; } = string.Empty;
    public string BrandKey { get; private set; } = string.Empty;

    public static Tenant Create(
        Guid id,
        string name,
        string brandKey,
        DateTimeOffset utcNow) =>
        new(id, name, brandKey, utcNow);
}
