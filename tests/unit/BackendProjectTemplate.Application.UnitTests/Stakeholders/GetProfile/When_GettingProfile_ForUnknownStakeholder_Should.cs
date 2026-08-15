using BackendProjectTemplate.Application.Stakeholders.Features.GetProfile;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Stakeholders.GetProfile;

public sealed class When_GettingProfile_ForUnknownStakeholder_Should
{
    [Fact]
    public async Task ReturnStakeholderNotFound()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns((StakeholderReadModel?)null);
        var sut = new GetProfileHandler(stakeholderReadModelRepository);

        var result = await sut.HandleAsync(
            new GetProfileQuery(new ActorContext(
                stakeholderId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7().ToString("N"),
                Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(GetProfileStatus.StakeholderNotFound);
        result.Profile.ShouldBeNull();
    }
}
