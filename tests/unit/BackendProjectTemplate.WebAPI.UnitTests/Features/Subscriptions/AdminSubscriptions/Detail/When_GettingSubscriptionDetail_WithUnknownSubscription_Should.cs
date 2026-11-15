using BackendProjectTemplate.WebAPI.Features.Subscriptions.AdminSubscriptions;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Detail;

public sealed class When_GettingSubscriptionDetail_WithUnknownSubscription_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new AdminSubscriptionsControllerTestContext();

        var result = await context.CreateSubscriptionsController().Get(Guid.CreateVersion7(), new GetSubscriptionDetailRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }
}
