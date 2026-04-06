using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Domain.UnitTests;

public sealed class WhenCreatingStakeholderType_ShouldSetNameKeyAndAuditFields
{
    [Fact]
    public void Verify()
    {
        const string rawName = " Individual Customer ";
        const string rawKey = " individual_customer ";
        const string expectedName = "Individual Customer";
        const string expectedKey = "individual_customer";

        var now = new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero);

        var stakeholderType = StakeholderType.Create(rawName, rawKey, now);

        stakeholderType.Name.ShouldBe(expectedName);
        stakeholderType.Key.ShouldBe(expectedKey);
        stakeholderType.CreatedAtUtc.ShouldBe(now);
        stakeholderType.UpdatedAtUtc.ShouldBe(now);
    }
}
