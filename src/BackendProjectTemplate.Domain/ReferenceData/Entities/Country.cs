using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.ReferenceData.Entities;

public sealed class Country : Entity
{
    private Country()
    {
    }

    private Country(
        string name,
        string shortCode,
        string? callingCode,
        string flagUrl,
        DateTimeOffset utcNow)
    {
        Name = name;
        ShortCode = shortCode;
        CallingCode = callingCode;
        FlagUrl = flagUrl;
        SetAuditDates(utcNow);
    }

    public string Name { get; private set; } = string.Empty;
    public string ShortCode { get; private set; } = string.Empty;
    public string? CallingCode { get; private set; }
    public string FlagUrl { get; private set; } = string.Empty;

    public static Country Create(
        string name,
        string shortCode,
        string? callingCode,
        string flagUrl,
        DateTimeOffset utcNow) =>
        new(name, shortCode, callingCode, flagUrl, utcNow);
}
