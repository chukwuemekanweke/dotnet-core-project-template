using BackendProjectTemplate.Application.Authentication.Features.CheckEmailExistence;
using BackendProjectTemplate.Domain.Authentication.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.CheckEmailExistence;

public sealed class When_CheckingEmailExistence_WithUnknownEmail_Should
{
    [Fact]
    public async Task ReturnDoesNotExist()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);

        var result = await context.CreateCheckEmailExistenceHandler().HandleAsync(
            new CheckEmailExistenceCommand(email),
            CancellationToken.None);

        result.Exists.ShouldBeFalse();
    }
}
