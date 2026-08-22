using BackendProjectTemplate.Application.Authentication.Features.SignUpOtp;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenVerifyingOtpWithValidCode_Should
{
    [Fact]
    public async Task MarkUserAsVerified()
    {
        var email = AuthenticationTestData.Email();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var otp = AuthenticationTestData.Otp();

        var context = new AuthenticationFlowTestContext();
        var accessToken = new AccessToken("access-token", context.Clock.GetUtcNow().AddMinutes(5));
        var refreshToken = new RefreshToken("refresh-token", context.Clock.GetUtcNow().AddDays(1));
        var user = context.CreateUser(email, firstName, lastName);
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), firstName, lastName);

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.TwoFactorOtpService.ValidateOtpAsync(
                user.Id,
                otp,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>())
            .Returns(true);
        context.IdentityService.UpdateAsync(Arg.Is<AppUser>(candidate => candidate.EmailConfirmed)).Returns(IdentityResult.Success);
        context.AccessTokenService.Generate(user, stakeholder.Id).Returns(accessToken);
        context.RefreshTokenService.IssueAsync(
                user,
                AuthenticationOtpDefaults.EmailConfirmationSessionLifetime,
                Arg.Any<CancellationToken>())
            .Returns(refreshToken);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateSignUpOtpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpOtpCommand(email, otp),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpOtpStatus.Success);
        result.Tokens.ShouldBe(new AuthenticationTokens(accessToken, refreshToken));
        user.EmailConfirmed.ShouldBeTrue();
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserEmailConfirmed>(message => message.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInSuccessful>(message => message.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}



