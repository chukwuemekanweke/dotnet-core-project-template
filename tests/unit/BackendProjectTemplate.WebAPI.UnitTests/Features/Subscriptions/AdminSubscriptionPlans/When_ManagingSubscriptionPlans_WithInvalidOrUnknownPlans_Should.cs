using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptionPlans;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptionPlans;

public sealed class When_ManagingSubscriptionPlans_WithInvalidOrUnknownPlans_Should
{
    [Fact]
    public async Task ReturnBadRequestOrNotFound()
    {
        var context = new AdminSubscriptionsControllerTestContext();
        var controller = context.CreatePlansController();

        var badInterval = await controller.Create(
            new CreateSubscriptionPlanRequest("GOLD", "Gold", 1000m, Guid.CreateVersion7(), "Weekly"), CancellationToken.None);
        var fractionalPrice = await controller.Create(
            new CreateSubscriptionPlanRequest("GOLD", "Gold", 10.5m, Guid.CreateVersion7(), "Monthly"), CancellationToken.None);
        var badCode = await controller.Create(
            new CreateSubscriptionPlanRequest("gold plan!", "Gold", 1000m, Guid.CreateVersion7(), "Monthly"), CancellationToken.None);
        var missingUpdate = await controller.Update(Guid.CreateVersion7(), new UpdateSubscriptionPlanRequest("Gold", 1000m), CancellationToken.None);
        var missingActivation = await controller.SetActivation(Guid.CreateVersion7(), new SetSubscriptionPlanActivationRequest(false), CancellationToken.None);

        badInterval.Result.ShouldBeOfType<BadRequestObjectResult>();
        fractionalPrice.Result.ShouldBeOfType<BadRequestObjectResult>();
        badCode.Result.ShouldBeOfType<BadRequestObjectResult>();
        missingUpdate.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        missingActivation.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
