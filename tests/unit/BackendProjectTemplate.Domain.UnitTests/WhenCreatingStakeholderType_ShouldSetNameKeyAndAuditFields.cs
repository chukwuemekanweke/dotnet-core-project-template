using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingStakeholderType_ShouldSetNameKeyAndAuditFields
{
    [Fact]
    public void Verify()
    {
        var tenantId = Guid.CreateVersion7();
        const string rawName = " Individual Customer ";
        const string rawKey = " individual_customer ";
        const string expectedName = "Individual Customer";
        const string expectedKey = "individual_customer";

        var now = new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero);

        var stakeholderType = StakeholderType.Create(tenantId, rawName, rawKey, now);

        stakeholderType.TenantId.ShouldBe(tenantId);
        stakeholderType.Name.ShouldBe(expectedName);
        stakeholderType.Key.ShouldBe(expectedKey);
        stakeholderType.CreatedAtUtc.ShouldBe(default);
        stakeholderType.UpdatedAtUtc.ShouldBe(default);
    }
}
