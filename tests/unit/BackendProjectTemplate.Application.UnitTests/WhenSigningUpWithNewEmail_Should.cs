using BackendProjectTemplate.Application.Authentication.Features.SignUp;
using BackendProjectTemplate.Application.UnitTests.Authentication;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using StakeholderDefaults = BackendProjectTemplate.Application.Authentication.Constants.StakeholderDefaults;

namespace BackendProjectTemplate.Application.UnitTests;

public sealed class WhenSigningUpWithNewEmail_Should
{
    [Fact]
    public async Task CreateIdentityUserAndPublishUserCreatedEvent()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var countryId = Guid.CreateVersion7();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var tenantId = Guid.CreateVersion7();
        var stakeholderType = StakeholderType.Create(
            tenantId,
            StakeholderDefaults.TypeName,
            StakeholderDefaults.TypeKey,
            context.Clock.GetUtcNow());

        context.CurrentActor.TenantId.Returns(tenantId);
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), password).Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var result = await context.CreateSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpCommand(
                email: email,
                password: password,
                countryId: countryId,
                firstName: firstName,
                lastName: lastName),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.Accepted);
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == email &&
                user.UserName == email),
            password);
        await context.StakeholderRepository.Received(1).AddAsync(
            Arg.Is<Domain.Stakeholders.Entities.Stakeholder>(stakeholder =>
                stakeholder.CountryId == countryId &&
                stakeholder.FirstName == firstName &&
                stakeholder.LastName == lastName),
            Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message =>
                message.StakeholderId != null),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}

