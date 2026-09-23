using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.LoginActivity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.LoginActivity;

public sealed class When_GettingLoginActivity_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnOkUsingCurrentActorScope()
    {
        var context = new LoginActivityControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        LoginActivityHistoryCursorRequest? capturedRequest = null;
        context.CurrentActor.ActorId.Returns(stakeholderId.ToString());
        context.CurrentActor.TenantId.Returns(tenantId);
        context.CurrentActor.CorrelationId.Returns("correlation-id");
        context.CurrentActor.FlowId.Returns("flow-id");
        context.Repository.GetByStakeholderAsync(
                Arg.Do<LoginActivityHistoryCursorRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new LoginActivityHistoryCursorPage(
                [new LoginActivityHistoryReadModel(
                    Guid.CreateVersion7(), LoginActivityType.InitialLogin, DateTimeOffset.UtcNow,
                    "Desktop", "Windows", "Chrome", "Lagos", "Lagos", "Nigeria")],
                false));

        var result = await context.CreateController().Handle(
            new GetLoginActivityRequest(),
            CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<LoginActivityHistoryResponse>().Activities.Count.ShouldBe(1);
        capturedRequest.ShouldNotBeNull();
        capturedRequest.StakeholderId.ShouldBe(stakeholderId);
        capturedRequest.TenantId.ShouldBe(tenantId);
    }
}
