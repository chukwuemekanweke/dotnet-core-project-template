using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingStakeholder_ShouldSetStakeholderTypeAndAuditFields
{
    [Fact]
    public void Verify()
    {
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderTypeId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero);

        var stakeholder = Stakeholder.Create(tenantId, countryId, stakeholderTypeId, now);

        stakeholder.TenantId.ShouldBe(tenantId);
        stakeholder.CountryId.ShouldBe(countryId);
        stakeholder.StakeholderTypeId.ShouldBe(stakeholderTypeId);
        stakeholder.CreatedAtUtc.ShouldBe(now);
        stakeholder.UpdatedAtUtc.ShouldBe(now);
    }
}
