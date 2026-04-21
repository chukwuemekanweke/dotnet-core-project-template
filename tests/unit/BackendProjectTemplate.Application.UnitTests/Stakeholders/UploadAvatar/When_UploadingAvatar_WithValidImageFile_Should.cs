using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Stakeholders.UploadAvatar;

public sealed class When_UploadingAvatar_WithValidImageFile_Should
{
    [Fact]
    public async Task PersistAvatarUrl()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 21, 12, 0, 0, TimeSpan.Zero));
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe",
            timeProvider.GetUtcNow());
        await using var stream = new MemoryStream([1, 2, 3]);

        currentActor.ActorId.Returns(stakeholderId.ToString());
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        objectStorageService.UploadPublicAsync(Arg.Any<ObjectStorageUploadRequest>(), Arg.Any<CancellationToken>())
            .Returns("https://example.com/avatar.png");

        var sut = new UploadAvatarHandler(
            currentActor,
            stakeholderRepository,
            objectStorageService,
            unitOfWork,
            timeProvider);

        var result = await sut.HandleAsync(
            new UploadAvatarCommand(stream, "avatar.png", "image/png", stream.Length),
            CancellationToken.None);

        result.Status.ShouldBe(UploadAvatarStatus.Success);
        stakeholder.AvatarUrl.ShouldBe("https://example.com/avatar.png");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
