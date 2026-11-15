using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Cancellations;

public sealed class When_CancellingSubscription_WithUnknownSubscriptionOrMissingReason_Should
{
    [Fact]
    public async Task ReturnNotFoundOrBadRequest()
    {
        var context = new AdminSubscriptionsControllerTestContext();
        var controller = context.CreateSubscriptionsController();
        var valid = new AdminSubscriptionCancellationRequest(Guid.CreateVersion7(), "Refund requested");

        var immediate = await controller.CancelImmediately(Guid.CreateVersion7(), valid, CancellationToken.None);
        var periodEnd = await controller.CancelAtPeriodEnd(Guid.CreateVersion7(), valid, CancellationToken.None);
        var missingReason = await controller.CancelImmediately(Guid.CreateVersion7(), valid with { Reason = "" }, CancellationToken.None);

        immediate.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        periodEnd.Result.ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        missingReason.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
