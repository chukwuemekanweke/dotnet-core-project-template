using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.List;

public sealed class When_ListingSubscriptions_WithInvalidFilters_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new AdminSubscriptionsControllerTestContext();
        var controller = context.CreateSubscriptionsController();

        (await controller.List(new ListSubscriptionsRequest(Status: "Paused"), CancellationToken.None)).Result.ShouldBeOfType<BadRequestObjectResult>();
        (await controller.List(new ListSubscriptionsRequest(Limit: 0), CancellationToken.None)).Result.ShouldBeOfType<BadRequestObjectResult>();
        (await controller.List(new ListSubscriptionsRequest(Cursor: "not-a-cursor"), CancellationToken.None)).Result.ShouldBeOfType<BadRequestObjectResult>();
        await context.ReadModels.DidNotReceiveWithAnyArgs().ListSubscriptionsAsync(default!, default);
    }
}
