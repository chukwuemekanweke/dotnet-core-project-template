using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.SecurityActivity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.SecurityActivity;

public sealed class When_GettingSecurityActivity_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnOkUsingCurrentActorScope()
    {
        var context = new SecurityActivityControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        SecurityActivityHistoryCursorRequest? capturedRequest = null;
        context.CurrentActor.ActorId.Returns(stakeholderId.ToString());
        context.CurrentActor.TenantId.Returns(tenantId);
        context.CurrentActor.CorrelationId.Returns("correlation-id");
        context.CurrentActor.FlowId.Returns("flow-id");
        context.Repository.GetByStakeholderAsync(
                Arg.Do<SecurityActivityHistoryCursorRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new SecurityActivityHistoryCursorPage(
                [new SecurityActivityHistoryReadModel(
                    Guid.CreateVersion7(), SecurityActivityType.InitialLogin, DateTimeOffset.UtcNow,
                    "203.0.113.10", "Desktop", "Windows", "Chrome", "Lagos", "Lagos", "Nigeria", null)],
                false));

        var result = await context.CreateController().Handle(
            new GetSecurityActivityRequest(),
            CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<SecurityActivityHistoryResponse>();
        response.Activities.Count.ShouldBe(1);
        response.Activities[0].IpAddress.ShouldBe("203.0.113.10");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.StakeholderId.ShouldBe(stakeholderId);
        capturedRequest.TenantId.ShouldBe(tenantId);
    }
}
