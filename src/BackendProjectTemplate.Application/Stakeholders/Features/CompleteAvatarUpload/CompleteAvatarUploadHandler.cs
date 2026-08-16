using BackendProjectTemplate.Contracts.Commands.Storage;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Application.Stakeholders.Features.CompleteAvatarUpload;

public sealed class CompleteAvatarUploadHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<AvatarUpload> avatarUploadRepository,
    IObjectStorageService objectStorageService,
    ICommandSender commandSender,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const long MaxAvatarFileSizeBytes = 2 * 1024 * 1024;

    public async Task<CompleteAvatarUploadResult> HandleAsync(
        CompleteAvatarUploadCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.ActorContext.StakeholderId.HasValue || !command.ActorContext.TenantId.HasValue)
        {
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.NotAuthenticated);
        }

        var stakeholderId = command.ActorContext.StakeholderId.Value;
        var tenantId = command.ActorContext.TenantId.Value;
        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken);
        if (stakeholder is null || stakeholder.TenantId != tenantId)
        {
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.StakeholderNotFound);
        }

        var upload = await avatarUploadRepository.FirstOrDefaultAsync(
            new AvatarUploadByIdAndOwnerSpecification(command.UploadId, stakeholderId, tenantId),
            cancellationToken);
        if (upload is null)
        {
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.UploadNotFound);
        }

        if (upload.Status == AvatarUploadStatus.Completed)
        {
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.Success, upload.FinalUrl);
        }

        if (upload.Status != AvatarUploadStatus.Pending)
        {
            return new CompleteAvatarUploadResult(
                upload.Status == AvatarUploadStatus.Expired
                    ? CompleteAvatarUploadStatus.Expired
                    : CompleteAvatarUploadStatus.InvalidFile);
        }

        if (timeProvider.GetUtcNow() > upload.ExpiresAtUtc)
        {
            upload.MarkExpired();
            await QueueQuarantineDeletionAsync(upload, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.Expired, Error: "Avatar upload has expired.");
        }

        var metadata = await objectStorageService.GetPrivateObjectMetadataAsync(
            upload.QuarantineObjectKey,
            cancellationToken);
        if (metadata is null ||
            metadata.ContentLength <= 0 ||
            metadata.ContentLength > MaxAvatarFileSizeBytes ||
            metadata.ContentLength != upload.ExpectedContentLength ||
            !string.Equals(metadata.ContentType.Trim(), upload.ExpectedContentType, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(metadata.ETag))
        {
            return await RejectAsync(upload, command, "invalid_metadata", cancellationToken);
        }

        byte[] signature;
        try
        {
            signature = await objectStorageService.ReadPrivateObjectRangeAsync(
                new ObjectStorageRangeReadRequest(
                    upload.QuarantineObjectKey,
                    0,
                    AvatarFileSignatureValidator.RequiredByteCount - 1,
                    metadata.ETag),
                cancellationToken);
        }
        catch (ObjectStoragePreconditionFailedException)
        {
            return await RejectChangedAsync(upload, command, cancellationToken);
        }

        if (!AvatarFileSignatureValidator.Matches(upload.ExpectedContentType, signature))
        {
            return await RejectAsync(upload, command, "invalid_signature", cancellationToken);
        }

        string finalUrl;
        try
        {
            finalUrl = await objectStorageService.PromotePrivateObjectToPublicAsync(
                new ObjectStoragePromotionRequest(
                    upload.QuarantineObjectKey,
                    upload.FinalObjectKey,
                    metadata.ETag,
                    upload.ExpectedContentType),
                cancellationToken);
        }
        catch (ObjectStoragePreconditionFailedException)
        {
            return await RejectChangedAsync(upload, command, cancellationToken);
        }

        stakeholder.SetAvatarUrl(finalUrl);
        upload.MarkCompleted(finalUrl, metadata.ETag);
        await QueueQuarantineDeletionAsync(upload, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadCompleted,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                stakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Common.UploadId] = upload.Id.ToString()
                }));

        return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.Success, finalUrl);
    }

    private async Task<CompleteAvatarUploadResult> RejectAsync(
        AvatarUpload upload,
        CompleteAvatarUploadCommand command,
        string reason,
        CancellationToken cancellationToken)
    {
        upload.Reject(reason);
        await QueueQuarantineDeletionAsync(upload, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        RecordFailure(command, upload, reason);
        return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.InvalidFile, Error: "Avatar upload is invalid.");
    }

    private async Task<CompleteAvatarUploadResult> RejectChangedAsync(
        AvatarUpload upload,
        CompleteAvatarUploadCommand command,
        CancellationToken cancellationToken)
    {
        const string reason = "object_changed";
        upload.Reject(reason);
        await QueueQuarantineDeletionAsync(upload, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        RecordFailure(command, upload, reason);
        return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.UploadChanged, Error: "Avatar upload changed during validation.");
    }

    private Task QueueQuarantineDeletionAsync(AvatarUpload upload, CancellationToken cancellationToken) =>
        commandSender.SendAsync(
            new DeleteQuarantinedAvatarObject(upload.Id, upload.QuarantineObjectKey)
            {
                StakeholderId = upload.StakeholderId,
                TenantId = upload.TenantId
            },
            cancellationToken);

    private void RecordFailure(CompleteAvatarUploadCommand command, AvatarUpload upload, string reason)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadFailed,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                upload.StakeholderId,
                reason,
                new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Common.UploadId] = upload.Id.ToString()
                }));
    }
}
