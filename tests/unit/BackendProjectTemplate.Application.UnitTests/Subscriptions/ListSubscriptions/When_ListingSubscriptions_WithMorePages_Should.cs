using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptions;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.ListSubscriptions;

public sealed class When_ListingSubscriptions_WithMorePages_Should
{
    [Fact]
    public async Task PassFiltersAndReturnACursorFromTheLastItem()
    {
        var repository = Substitute.For<ISubscriptionAdministrationReadModelRepository>();
        var item = new AdminSubscriptionListItemReadModel(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "Tenant", Guid.CreateVersion7(), BillingAccountOwnerType.Stakeholder, Guid.CreateVersion7(), "Ada",
            Guid.CreateVersion7(), "GOLD", "Gold", SubscriptionStatus.Scheduled, null, null, AdminSubscriptionTestContext.Now.AddDays(1),
            AdminSubscriptionTestContext.Now.AddDays(31), null, false, false, AdminSubscriptionTestContext.Now, SubscriptionOrigin.Allocation,
            SubscriptionAllocationSource.Trial);
        AdminSubscriptionListRequest? captured = null;
        repository.ListSubscriptionsAsync(Arg.Do<AdminSubscriptionListRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns(new AdminSubscriptionListPage([item], true));
        var cursor = CursorPagination.Encode(AdminSubscriptionTestContext.Now.AddDays(1), Guid.CreateVersion7());

        var result = await new ListSubscriptionsHandler(repository).HandleAsync(
            new ListSubscriptionsCommand(
                item.TenantId, null, BillingAccountOwnerType.Stakeholder, null, null, SubscriptionStatus.Scheduled,
                SubscriptionAllocationSource.Trial, false, null, null, null, null, null, cursor, 1),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.TenantId.ShouldBe(item.TenantId);
        captured.Status.ShouldBe(SubscriptionStatus.Scheduled);
        captured.AllocationSource.ShouldBe(SubscriptionAllocationSource.Trial);
        captured.CursorSubscriptionId.ShouldNotBeNull();
        captured.Limit.ShouldBe(1);
        var response = result.Items.ShouldHaveSingleItem();
        response.Status.ShouldBe("Scheduled");
        response.Origin.ShouldBe("Allocation");
        response.AllocationSource.ShouldBe("Trial");
        CursorPagination.Decode(result.NextCursor).EntityId.ShouldBe(item.SubscriptionId);
    }
}
