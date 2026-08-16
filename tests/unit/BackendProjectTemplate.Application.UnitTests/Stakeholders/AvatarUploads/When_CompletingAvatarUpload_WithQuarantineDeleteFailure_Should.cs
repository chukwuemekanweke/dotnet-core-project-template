using BackendProjectTemplate.Application.Stakeholders.Features.CompleteAvatarUpload;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithQuarantineDeleteFailure_Should
{
    [Fact]
    public async Task ReturnSuccessAfterCommit()
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload();
        context.AvatarUploadRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<AvatarUpload>>(), Arg.Any<CancellationToken>()).Returns(upload);
        context.ObjectStorageService.GetPrivateObjectMetadataAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(12, "image/png", "etag-a"));
        context.ObjectStorageService.ReadPrivateObjectRangeAsync(Arg.Any<ObjectStorageRangeReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 });
        context.ObjectStorageService.PromotePrivateObjectToPublicAsync(Arg.Any<ObjectStoragePromotionRequest>(), Arg.Any<CancellationToken>())
            .Returns("https://cdn.example/avatar.png");
        context.ObjectStorageService.DeletePrivateObjectAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("cleanup failed"));

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.Success);
        upload.Status.ShouldBe(AvatarUploadStatus.Completed);
    }
}
