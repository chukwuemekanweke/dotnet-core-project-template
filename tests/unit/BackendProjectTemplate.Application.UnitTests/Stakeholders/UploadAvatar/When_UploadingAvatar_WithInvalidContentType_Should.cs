using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Stakeholders.UploadAvatar;

public sealed class When_UploadingAvatar_WithInvalidContentType_Should
{
    [Fact]
    public async Task ReturnInvalidFile()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());

        await using var stream = new MemoryStream([1, 2, 3]);
        var sut = new UploadAvatarHandler(
            currentActor,
            Substitute.For<IRepository<Stakeholder>>(),
            Substitute.For<IObjectStorageService>(),
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System);

        var result = await sut.HandleAsync(
            new UploadAvatarCommand(stream, "avatar.txt", "text/plain", stream.Length),
            CancellationToken.None);

        result.Status.ShouldBe(UploadAvatarStatus.InvalidFile);
    }
}
