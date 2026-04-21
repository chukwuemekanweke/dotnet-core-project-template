using BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Stakeholders.UpdateProfile;

public sealed class When_UpdatingProfile_WithBlankNames_Should
{
    [Fact]
    public async Task ReturnValidationFailure()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());

        var sut = new UpdateProfileHandler(
            currentActor,
            Substitute.For<IRepository<Stakeholder>>(),
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System);

        var result = await sut.HandleAsync(
            new UpdateProfileCommand(" ", " "),
            CancellationToken.None);

        result.Status.ShouldBe(UpdateProfileStatus.ValidationFailed);
    }
}
