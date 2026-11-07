using BackendProjectTemplate.Application.Authentication.Features.CreatePasskeyAuthenticationOptions;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Domain.Common.Authentication;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenCreatingPasskeyAuthenticationOptions_Should
{
    [Fact]
    public async Task BindOptionsToRequestedTenant()
    {
        var context = new AuthenticationFlowTestContext();
        var tenantId = Guid.CreateVersion7();
        var expected = new PasskeyOptions("flow", "{}", context.Clock.GetUtcNow().AddMinutes(5));
        context.PasskeyService.CreateAuthenticationOptionsAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await context.CreatePasskeyAuthenticationOptionsHandler().HandleAsync(
            new CreatePasskeyAuthenticationOptionsCommand(tenantId), CancellationToken.None);

        result.ShouldBe(expected);
        await context.PasskeyService.Received(1)
            .CreateAuthenticationOptionsAsync(tenantId, Arg.Any<CancellationToken>());
    }
}
