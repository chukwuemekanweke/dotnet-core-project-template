using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Allocations;

public sealed class When_AllocatingSubscription_WithAutoRenewRequested_Should
{
    [Fact]
    public async Task ReturnBadRequestWithoutCallingTheHandler()
    {
        var context = new AdminSubscriptionsControllerTestContext();
        var request = AdminSubscriptionsControllerTestContext.CreateAllocationRequest(Guid.CreateVersion7(), autoRenew: true);

        var result = await context.CreateSubscriptionsController().Allocate(request, CancellationToken.None);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBeOfType<ValidationProblemDetails>().Errors.ShouldContainKey(nameof(request.AutoRenew));
        await context.PlanRepository.DidNotReceiveWithAnyArgs().FirstOrDefaultAsync(default!, default);
    }
}
