using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Payments.BillingAccounts;

public sealed class When_CreatingBillingAccount_WithStakeholderOwner_Should
{
    [Fact]
    public void SetStakeholderOwnership()
    {
        var tenantId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();

        var account = BillingAccount.Create(
            tenantId,
            BillingAccountOwnerType.Stakeholder,
            stakeholderId,
            "Ada Lovelace");

        account.TenantId.ShouldBe(tenantId);
        account.OwnerType.ShouldBe(BillingAccountOwnerType.Stakeholder);
        account.StakeholderId.ShouldBe(stakeholderId);
    }
}
