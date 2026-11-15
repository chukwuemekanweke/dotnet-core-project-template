using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Allocations;

public sealed class When_AllocatingSubscription_WithNewAllocation_Should
{
    [Fact]
    public async Task ReturnCreatedWithANonRenewingGrant()
    {
        var context = new AdminSubscriptionsControllerTestContext();
        var plan = context.ReturnActivePlan();
        var request = AdminSubscriptionsControllerTestContext.CreateAllocationRequest(plan.Id);
        context.BillingAccountResolver.ResolveOrCreateAsync(request.TenantId, BillingAccountOwnerType.Tenant, null, Arg.Any<CancellationToken>())
            .Returns(BillingAccount.Create(request.TenantId, BillingAccountOwnerType.Tenant, null, "Tenant"));

        var result = await context.CreateSubscriptionsController().Allocate(request, CancellationToken.None);

        var created = result.Result.ShouldBeOfType<CreatedResult>();
        var response = created.Value.ShouldBeOfType<AllocateSubscriptionResponse>();
        response.AutoRenew.ShouldBeFalse();
        response.AllocationSource.ShouldBe("Promotion");
        created.Location.ShouldBe(EndpointUrl.AdminSubscriptions.DetailV1(response.SubscriptionId));
    }
}
