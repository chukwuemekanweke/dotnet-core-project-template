using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;

namespace BackendProjectTemplate.Domain.UnitTests.Payments.BillingAccounts;

public sealed class When_CreatingBillingAccount_WithInvalidDisplayName_Should
{
    [Fact]
    public void NormalizeAndValidateDisplayName()
    {
        var account = BillingAccount.Create(
            Guid.CreateVersion7(),
            BillingAccountOwnerType.Tenant,
            null,
            "  Example Tenant  ");

        account.DisplayName.ShouldBe("Example Tenant");
        Should.Throw<ArgumentException>(() => BillingAccount.Create(
            Guid.CreateVersion7(),
            BillingAccountOwnerType.Tenant,
            null,
            "   "));
        Should.Throw<ArgumentException>(() => BillingAccount.Create(
            Guid.CreateVersion7(),
            BillingAccountOwnerType.Tenant,
            null,
            new string('a', BillingAccount.MaxDisplayNameLength + 1)));
    }
}
