using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Subscriptions.AdminSubscriptions.Allocations;

public sealed class When_AllocatingSubscription_WithInvalidOwner_Should
{
    [Theory]
    [InlineData("Wallet", null)]
    [InlineData("2", null)]
    [InlineData("Stakeholder", null)]
    [InlineData("Tenant", "019a86a6-2eed-7a1d-a8f0-f78309f45899")]
    public async Task ReturnBadRequest(string ownerType, string? stakeholderId)
    {
        var context = new AdminSubscriptionsControllerTestContext();
        var request = AdminSubscriptionsControllerTestContext.CreateAllocationRequest(Guid.CreateVersion7()) with
        {
            BillingAccountOwnerType = ownerType,
            StakeholderId = stakeholderId is null ? null : Guid.Parse(stakeholderId)
        };

        var result = await context.CreateSubscriptionsController().Allocate(request, CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
