using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Stakeholders.UploadAvatar;

public sealed class When_UploadingAvatar_ForMissingStakeholder_Should
{
    [Fact]
    public async Task ReturnStakeholderNotFound()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var stakeholderId = Guid.CreateVersion7();
        await using var stream = new MemoryStream([1, 2, 3]);

        currentActor.ActorId.Returns(stakeholderId.ToString());
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns((Stakeholder?)null);

        var sut = new UploadAvatarHandler(
            currentActor,
            stakeholderRepository,
            Substitute.For<IObjectStorageService>(),
            customTelemetryContext,
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System);

        var result = await sut.HandleAsync(
            new UploadAvatarCommand(stream, "avatar.png", "image/png", stream.Length),
            CancellationToken.None);

        result.Status.ShouldBe(UploadAvatarStatus.StakeholderNotFound);
    }
}
