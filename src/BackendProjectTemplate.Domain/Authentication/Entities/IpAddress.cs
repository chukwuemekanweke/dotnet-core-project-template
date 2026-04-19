using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Authentication.Entities;

public sealed class IpAddress : Entity
{
    private const int MaxValueLength = 45;

    private IpAddress()
    {
    }

    private IpAddress(string value)
    {
        Value = NormalizeValue(value);
    }

    public string Value { get; private set; } = string.Empty;
    public DateTimeOffset? LocationLookupTimestampUtc { get; private set; }
    public ICollection<IpAddressLocation> Locations { get; private set; } = [];

    public static IpAddress Create(string value) => new(value);

    public void RecordLocationLookup(DateTimeOffset lookedUpAtUtc)
    {
        LocationLookupTimestampUtc = lookedUpAtUtc;
    }

    public IpAddressLocation? GetCurrentLocation() =>
        Locations.FirstOrDefault(location => location.IsCurrentLocation);

    public void ApplyLocationResolution(
        string? city,
        string? state,
        string? country,
        DateTimeOffset resolvedAtUtc)
    {
        RecordLocationLookup(resolvedAtUtc);

        var currentLocation = GetCurrentLocation();
        if (currentLocation is null)
        {
            Locations.Add(IpAddressLocation.Create(Id, city, state, country, resolvedAtUtc));
            return;
        }

        if (currentLocation.Matches(city, state, country))
        {
            return;
        }

        currentLocation.MarkAsHistorical();
        Locations.Add(IpAddressLocation.Create(Id, city, state, country, resolvedAtUtc));
    }

    private static string NormalizeValue(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("IP address is required.", nameof(value));
        }

        if (normalized.Length > MaxValueLength)
        {
            throw new ArgumentException($"IP address must not exceed {MaxValueLength} characters.", nameof(value));
        }

        return normalized;
    }
}
