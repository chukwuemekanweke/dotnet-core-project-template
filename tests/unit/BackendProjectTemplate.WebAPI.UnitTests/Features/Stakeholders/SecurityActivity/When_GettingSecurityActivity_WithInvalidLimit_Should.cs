using BackendProjectTemplate.WebAPI.Features.Stakeholders.SecurityActivity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.SecurityActivity;

public sealed class When_GettingSecurityActivity_WithInvalidLimit_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new SecurityActivityControllerTestContext();

        var result = await context.CreateController().Handle(
            new GetSecurityActivityRequest(0),
            CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
