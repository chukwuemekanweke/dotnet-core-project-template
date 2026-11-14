using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Payments.BillingAccounts;

public sealed class When_CreatingBillingAccount_WithTenantOwner_Should
{
    [Fact]
    public void SetImmutableOwnershipAndDefaultAuditFields()
    {
        var tenantId = Guid.CreateVersion7();

        var account = BillingAccount.Create(
            tenantId,
            BillingAccountOwnerType.Tenant,
            null,
            "Example Tenant");

        account.TenantId.ShouldBe(tenantId);
        account.OwnerType.ShouldBe(BillingAccountOwnerType.Tenant);
        account.StakeholderId.ShouldBeNull();
        account.CreatedAtUtc.ShouldBe(default);
        account.UpdatedAtUtc.ShouldBe(default);
        typeof(BillingAccount).GetProperty(nameof(BillingAccount.TenantId))!.SetMethod!.IsPrivate.ShouldBeTrue();
        typeof(BillingAccount).GetProperty(nameof(BillingAccount.OwnerType))!.SetMethod!.IsPrivate.ShouldBeTrue();
        typeof(BillingAccount).GetProperty(nameof(BillingAccount.StakeholderId))!.SetMethod!.IsPrivate.ShouldBeTrue();
    }
}
