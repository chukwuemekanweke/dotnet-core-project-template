using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Allocations;

public sealed class When_AllocatingSubscription_WithMissingOrInactivePlan_Should
{
    [Fact]
    public async Task ReturnNotFoundOrUnprocessable()
    {
        var context = new AdminSubscriptionsControllerTestContext();
        var controller = context.CreateSubscriptionsController();

        var missing = await controller.Allocate(AdminSubscriptionsControllerTestContext.CreateAllocationRequest(Guid.CreateVersion7()), CancellationToken.None);
        var plan = context.ReturnActivePlan();
        plan.SetActive(false);
        var inactive = await controller.Allocate(AdminSubscriptionsControllerTestContext.CreateAllocationRequest(plan.Id), CancellationToken.None);

        missing.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        inactive.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }
}
