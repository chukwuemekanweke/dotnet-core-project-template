using BackendProjectTemplate.Application.Stakeholders.Features.CompleteAvatarUpload;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using Shouldly;

namespace BackendProjectTemplate.Application.UnitTests.Stakeholders.AvatarUploads;

public sealed class When_CompletingAvatarUpload_WithCompletedUpload_Should
{
    [Fact]
    public async Task ReturnPersistedUrlWithoutStorageCalls()
    {
        var context = new AvatarUploadHandlerTestContext();
        var upload = context.PendingUpload();
        upload.MarkCompleted("https://cdn.example/avatar.png", "etag-a");
        context.AvatarUploadRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<AvatarUpload>>(), Arg.Any<CancellationToken>())
            .Returns(upload);

        var result = await context.CompleteHandler().HandleAsync(
            new CompleteAvatarUploadCommand(upload.Id, context.ActorContext()),
            CancellationToken.None);

        result.Status.ShouldBe(CompleteAvatarUploadStatus.Success);
        result.AvatarUrl.ShouldBe(upload.FinalUrl);
        await context.ObjectStorageService.DidNotReceiveWithAnyArgs()
            .PromotePrivateObjectToPublicAsync(default!, default);
    }
}
