using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenLinkingAppUserToStakeholder_ShouldSetKeysAndCreatedTimestamp
{
    [Fact]
    public void Verify()
    {
        var appUserId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var createdAtUtc = new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero);

        var appUserStakeholder = AppUserStakeholder.Create(appUserId, stakeholderId, createdAtUtc);

        appUserStakeholder.AppUserId.ShouldBe(appUserId);
        appUserStakeholder.StakeholderId.ShouldBe(stakeholderId);
        appUserStakeholder.CreatedAtUtc.ShouldBe(createdAtUtc);
        appUserStakeholder.UpdatedAtUtc.ShouldBe(createdAtUtc);
    }
}
