using BackendProjectTemplate.WebAPI.Features.Stakeholders.SecurityActivity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.SecurityActivity;

public sealed class When_GettingSecurityActivity_WithoutAuthenticatedActor_Should
{
    [Fact]
    public async Task ReturnUnauthorizedWithoutQueryingRepository()
    {
        var context = new SecurityActivityControllerTestContext();
        context.CurrentActor.TenantId.Returns(Guid.CreateVersion7());

        var result = await context.CreateController().Handle(
            new GetSecurityActivityRequest(),
            CancellationToken.None);

        result.Result.ShouldBeOfType<UnauthorizedResult>();
        await context.Repository.DidNotReceiveWithAnyArgs()
            .GetByStakeholderAsync(default!, default);
    }
}
