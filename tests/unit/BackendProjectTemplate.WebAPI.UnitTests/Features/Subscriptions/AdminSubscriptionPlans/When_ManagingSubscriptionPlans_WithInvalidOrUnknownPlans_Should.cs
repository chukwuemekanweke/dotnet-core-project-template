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
            new CreateSubscriptionPlanRequest("GOLD", "Gold", [new("Weekly", 1000m)], Guid.CreateVersion7()), CancellationToken.None);
        var fractionalPrice = await controller.Create(
            new CreateSubscriptionPlanRequest("GOLD", "Gold", [new("Monthly", 10.5m)], Guid.CreateVersion7()), CancellationToken.None);
        var badCode = await controller.Create(
            new CreateSubscriptionPlanRequest("gold plan!", "Gold", [new("Monthly", 1000m)], Guid.CreateVersion7()), CancellationToken.None);
        var noPrices = await controller.Create(
            new CreateSubscriptionPlanRequest("GOLD", "Gold", [], Guid.CreateVersion7()), CancellationToken.None);
        var duplicateInterval = await controller.Create(
            new CreateSubscriptionPlanRequest("GOLD", "Gold", [new("Monthly", 1000m), new("monthly", 2000m)], Guid.CreateVersion7()), CancellationToken.None);
        var blankFeature = await controller.Create(
            new CreateSubscriptionPlanRequest("GOLD", "Gold", [new("Monthly", 1000m)], Guid.CreateVersion7(), Features: [" "]), CancellationToken.None);
        var missingUpdate = await controller.Update(Guid.CreateVersion7(), new UpdateSubscriptionPlanRequest("Gold", [new("Monthly", 1000m)]), CancellationToken.None);
        var missingActivation = await controller.SetActivation(Guid.CreateVersion7(), new SetSubscriptionPlanActivationRequest(false), CancellationToken.None);

        badInterval.Result.ShouldBeOfType<BadRequestObjectResult>();
        fractionalPrice.Result.ShouldBeOfType<BadRequestObjectResult>();
        badCode.Result.ShouldBeOfType<BadRequestObjectResult>();
        noPrices.Result.ShouldBeOfType<BadRequestObjectResult>();
        duplicateInterval.Result.ShouldBeOfType<BadRequestObjectResult>();
        blankFeature.Result.ShouldBeOfType<BadRequestObjectResult>();
        missingUpdate.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        missingActivation.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
