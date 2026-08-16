using BackendProjectTemplate.Application.Stakeholders.Features.GetProfile;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_GettingProfile_WithAuthenticatedStakeholder_Should
{
    [Fact]
    public async Task ReturnProfile()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var stakeholder = new StakeholderReadModel(
            stakeholderId,
            Guid.CreateVersion7(),
            "ada@example.com",
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Ada",
            "Lovelace",
            "https://cdn.example.com/avatars/ada.jpg",
            true);
        currentActor.ActorId.Returns(stakeholderId.ToString());
        currentActor.TenantId.Returns(tenantId);
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        var sut = CreateController(stakeholderReadModelRepository, currentActor);

        var result = await sut.GetProfile(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(new GetProfileResponse(
            stakeholderId,
            stakeholder.EmailAddress,
            stakeholder.FirstName,
            stakeholder.LastName,
            stakeholder.AvatarUrl,
            stakeholder.IsVerified));
    }

    private static ProfilesController CreateController(
        IStakeholderReadModelRepository stakeholderReadModelRepository,
        ICurrentActor currentActor)
    {
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        return ProfilesControllerTestFactory.Create(
            stakeholderReadModelRepository,
            stakeholderRepository,
            customTelemetryContext,
            unitOfWork,
            currentActor);
    }
}
