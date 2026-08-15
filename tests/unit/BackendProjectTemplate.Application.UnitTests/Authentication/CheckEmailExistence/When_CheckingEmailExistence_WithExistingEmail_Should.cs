using BackendProjectTemplate.Application.Authentication.Features.CheckEmailExistence;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.CheckEmailExistence;

public sealed class When_CheckingEmailExistence_WithExistingEmail_Should
{
    [Fact]
    public async Task ReturnExists()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        context.IdentityService.FindByEmailAsync(email).Returns(context.CreateUser(email));

        var result = await context.CreateCheckEmailExistenceHandler().HandleAsync(
            new CheckEmailExistenceCommand(email),
            CancellationToken.None);

        result.Exists.ShouldBeTrue();
    }
}
