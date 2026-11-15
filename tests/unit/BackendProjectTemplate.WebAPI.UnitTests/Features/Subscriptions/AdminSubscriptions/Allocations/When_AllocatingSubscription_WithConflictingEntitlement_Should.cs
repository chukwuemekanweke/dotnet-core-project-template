using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Allocations;

public sealed class When_AllocatingSubscription_WithConflictingEntitlement_Should
{
    [Fact]
    public async Task ReturnConflict()
    {
        var context = new AdminSubscriptionsControllerTestContext();
        var plan = context.ReturnActivePlan();
        var request = AdminSubscriptionsControllerTestContext.CreateAllocationRequest(plan.Id);
        context.BillingAccountResolver.ResolveOrCreateAsync(request.TenantId, BillingAccountOwnerType.Tenant, null, Arg.Any<CancellationToken>())
            .Returns(BillingAccount.Create(request.TenantId, BillingAccountOwnerType.Tenant, null, "Tenant"));
        context.SubscriptionRepository.AnyAsync(Arg.Any<ISpecification<SubscriptionAggregate>>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await context.CreateSubscriptionsController().Allocate(request, CancellationToken.None);

        result.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }
}
