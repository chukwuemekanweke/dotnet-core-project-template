using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;
using StakeholderDefaults = BackendProjectTemplate.Application.Authentication.Constants.StakeholderDefaults;

namespace BackendProjectTemplate.Application.UnitTests.Authentication.SignUp;

public sealed class When_SigningUp_WithUnavailableIpGeolocation_Should
{
    [Fact]
    public async Task ContinueRegistration()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateSignUpCommand();
        var stakeholderType = StakeholderType.Create(
            command.ActorContext.TenantId!.Value,
            StakeholderDefaults.TypeName,
            StakeholderDefaults.TypeKey);
        context.IpGeolocationService.GetGeolocationAsync(command.IpAddress, Arg.Any<CancellationToken>())
            .Returns((IpGeolocation?)null);
        context.IdentityService.FindByEmailAsync(command.Email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), command.Password).Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var result = await context.CreateSignUpHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.Accepted);
        await context.IdentityService.Received(1).CreateAsync(Arg.Any<AppUser>(), command.Password);
    }
}
