using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.AllocateSubscription;

public sealed class When_AllocatingSubscription_WithStakeholderOwnedBillingAccount_Should
{
    [Fact]
    public async Task ResolveTheStakeholderAccountWithinTheTargetTenant()
    {
        var context = new AdminSubscriptionTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var account = BillingAccount.Create(context.TenantId, BillingAccountOwnerType.Stakeholder, stakeholderId, "Ada Lovelace");
        context.BillingAccountResolver.ResolveOrCreateAsync(context.TenantId, BillingAccountOwnerType.Stakeholder, stakeholderId, Arg.Any<CancellationToken>())
            .Returns(account);

        var result = await context.CreateAllocateHandler().HandleAsync(
            context.CreateAllocation(ownerType: BillingAccountOwnerType.Stakeholder, stakeholderId: stakeholderId),
            CancellationToken.None);

        result.Status.ShouldBe(AllocateSubscriptionStatus.Allocated);
        result.Response!.BillingAccountId.ShouldBe(account.Id);
        result.Response.TenantId.ShouldBe(context.TenantId);
    }
}
