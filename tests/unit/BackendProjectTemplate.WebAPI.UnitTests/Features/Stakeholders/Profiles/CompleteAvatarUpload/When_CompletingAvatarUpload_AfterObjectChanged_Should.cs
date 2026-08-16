using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Stakeholders.Profiles.CompleteAvatarUpload;

public sealed class When_CompletingAvatarUpload_AfterObjectChanged_Should
{
    [Fact]
    public async Task ReturnConflict()
    {
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var uploadRepository = Substitute.For<IRepository<AvatarUpload>>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe");
        var uploadId = Guid.CreateVersion7();
        var upload = AvatarUpload.Create(
            uploadId,
            stakeholderId,
            tenantId,
            "avatar.png",
            "image/png",
            12,
            ".png",
            "quarantine/avatar.png",
            "avatar/avatar.png",
            TimeProvider.System.GetUtcNow().AddMinutes(10));
        currentActor.ActorId.Returns(stakeholderId.ToString());
        currentActor.TenantId.Returns(tenantId);
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>()).Returns(stakeholder);
        uploadRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<AvatarUpload>>(), Arg.Any<CancellationToken>()).Returns(upload);
        objectStorageService.GetPrivateObjectMetadataAsync(upload.QuarantineObjectKey, Arg.Any<CancellationToken>())
            .Returns(new ObjectStorageObjectMetadata(12, "image/png", "etag-a"));
        objectStorageService.ReadPrivateObjectRangeAsync(Arg.Any<ObjectStorageRangeReadRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<byte[]>>(_ => throw new ObjectStoragePreconditionFailedException("changed"));
        var sut = ProfilesControllerTestFactory.Create(
            Substitute.For<IStakeholderReadModelRepository>(),
            stakeholderRepository,
            Substitute.For<ICustomTelemetryContext>(),
            Substitute.For<IUnitOfWork>(),
            currentActor,
            objectStorageService,
            uploadRepository);

        var result = await sut.CompleteAvatarUpload(uploadId, CancellationToken.None);

        result.Result.ShouldBeOfType<ConflictObjectResult>();
    }
}
