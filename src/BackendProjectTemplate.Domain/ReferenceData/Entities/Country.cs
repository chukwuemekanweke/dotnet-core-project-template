using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.ReferenceData.Entities;

public sealed class Country : Entity
{
    private Country()
    {
    }

    private Country(string code, string name, DateTimeOffset utcNow)
    {
        Code = code;
        Name = name;
        SetAuditDates(utcNow);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public static Country Create(string code, string name, DateTimeOffset utcNow) =>
        new(code, name, utcNow);
}
