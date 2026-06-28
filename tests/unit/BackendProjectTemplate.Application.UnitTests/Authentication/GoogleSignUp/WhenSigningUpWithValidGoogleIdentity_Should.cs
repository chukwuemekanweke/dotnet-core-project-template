using BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using StakeholderDefaults = BackendProjectTemplate.Application.Authentication.Constants.StakeholderDefaults;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningUpWithValidGoogleIdentity_Should
{
    [Fact]
    public async Task ConfirmEmailAndPublishUserCreatedEvent()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var subject = Guid.CreateVersion7().ToString("N");
        var countryId = Guid.CreateVersion7();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var tenantId = Guid.CreateVersion7();
        var stakeholderType = StakeholderType.Create(tenantId, StakeholderDefaults.TypeName, StakeholderDefaults.TypeKey);

        context.GoogleIdentityTokenService.ValidateAsync("google-id-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentityTokenPayload(subject, email, "Google User"));
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>())
            .Returns(IdentityResult.Success);
        context.IdentityService.AddLoginAsync(
                Arg.Any<AppUser>(),
                Arg.Any<string>(),
                subject,
                Arg.Any<string>())
            .Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var result = await context.CreateGoogleSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateGoogleSignUpCommand(
                idToken: "google-id-token",
                countryId: countryId,
                firstName: firstName,
                lastName: lastName),
            CancellationToken.None);

        result.Status.ShouldBe(GoogleSignUpStatus.Accepted);
        result.Email.ShouldBe(email);
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == email &&
                user.UserName == email &&
                user.EmailConfirmed));
        await context.IdentityService.Received(1).AddLoginAsync(
            Arg.Any<AppUser>(),
            "Google",
            subject,
            "Google");
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message => message.StakeholderId != null),
            Arg.Any<CancellationToken>());
    }
}



