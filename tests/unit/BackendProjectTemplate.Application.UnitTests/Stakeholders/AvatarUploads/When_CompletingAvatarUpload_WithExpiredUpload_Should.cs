using BackendProjectTemplate.Application.Stakeholders.Features.CompleteAvatarUpload;
using BackendProjectTemplate.Contracts.Commands.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithExpiredUpload_Should
{
    [Fact]
    public async Task MarkExpiredAndQueueQuarantineDeletion()
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload(expiresAtUtc: context.UtcNow.AddSeconds(-1));
        context.AvatarUploadRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<AvatarUpload>>(), Arg.Any<CancellationToken>())
            .Returns(upload);

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.Expired);
        upload.Status.ShouldBe(AvatarUploadStatus.Expired);
        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<DeleteQuarantinedAvatarObject>(command => command.ObjectKey == upload.QuarantineObjectKey),
            Arg.Any<CancellationToken>());
    }
}
