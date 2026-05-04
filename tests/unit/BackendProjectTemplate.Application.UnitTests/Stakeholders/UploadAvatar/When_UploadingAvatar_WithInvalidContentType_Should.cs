using BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
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
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());

        await using var stream = new MemoryStream([1, 2, 3]);
        var sut = new UploadAvatarHandler(
            Substitute.For<IRepository<Stakeholder>>(),
            Substitute.For<IObjectStorageService>(),
            customTelemetryContext,
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System);

        var result = await sut.HandleAsync(
            new UploadAvatarCommand(stream, "avatar.txt", "text/plain", stream.Length, new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(UploadAvatarStatus.InvalidFile);
        customTelemetryContext.Received().AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadFailed,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.InvalidFile));
    }
}
