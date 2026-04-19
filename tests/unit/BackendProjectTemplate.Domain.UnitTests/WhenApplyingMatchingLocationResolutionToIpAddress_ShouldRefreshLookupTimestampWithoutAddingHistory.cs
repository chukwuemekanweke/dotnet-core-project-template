using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenApplyingMatchingLocationResolutionToIpAddress_ShouldRefreshLookupTimestampWithoutAddingHistory
{
    [Fact]
    public void Verify()
    {
        var firstLookupAtUtc = new DateTimeOffset(2026, 4, 19, 10, 0, 0, TimeSpan.Zero);
        var secondLookupAtUtc = firstLookupAtUtc.AddDays(7);
        var ipAddress = IpAddress.Create("203.0.113.10");

        ipAddress.ApplyLocationResolution("Lagos", "Lagos", "Nigeria", firstLookupAtUtc);
        ipAddress.ApplyLocationResolution("Lagos", "Lagos", "Nigeria", secondLookupAtUtc);

        ipAddress.LocationLookupTimestampUtc.ShouldBe(secondLookupAtUtc);
        ipAddress.Locations.Count.ShouldBe(1);

        var currentLocation = ipAddress.GetCurrentLocation();
        currentLocation.ShouldNotBeNull();
        currentLocation.City.ShouldBe("Lagos");
        currentLocation.State.ShouldBe("Lagos");
        currentLocation.Country.ShouldBe("Nigeria");
        currentLocation.IsCurrentLocation.ShouldBeTrue();
        currentLocation.ResolvedAtUtc.ShouldBe(firstLookupAtUtc);
    }
}
