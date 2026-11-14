using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Payments.BillingAccounts;

public sealed class When_CreatingBillingAccount_WithInvalidOwnership_Should
{
    [Fact]
    public void RejectInvalidOwnerCombinations()
    {
        var tenantId = Guid.CreateVersion7();

        Should.Throw<ArgumentException>(() => BillingAccount.Create(
            tenantId,
            BillingAccountOwnerType.Tenant,
            Guid.CreateVersion7(),
            "Tenant"));
        Should.Throw<ArgumentException>(() => BillingAccount.Create(
            tenantId,
            BillingAccountOwnerType.Stakeholder,
            null,
            "Stakeholder"));
        Should.Throw<ArgumentOutOfRangeException>(() => BillingAccount.Create(
            tenantId,
            (BillingAccountOwnerType)999,
            null,
            "Invalid"));
        Should.Throw<ArgumentException>(() => BillingAccount.Create(
            Guid.Empty,
            BillingAccountOwnerType.Tenant,
            null,
            "Tenant"));
    }
}
