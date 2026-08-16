using BackendProjectTemplate.Application.Common.FileUploads;
using BackendProjectTemplate.Application.Stakeholders.AvatarUploads;
using BackendProjectTemplate.Contracts.Commands.Storage;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Application.Stakeholders.Features.CompleteAvatarUpload;

public sealed class CompleteAvatarUploadHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<AvatarUpload> avatarUploadRepository,
    FileUploadService fileUploadService,
    ICommandSender commandSender,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
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

        var completion = await fileUploadService.CompleteAsync(
            new FileUploadCompletionRequest(
                upload.QuarantineObjectKey,
                upload.FinalObjectKey,
                upload.ExpectedContentType,
                upload.ExpectedContentLength),
            AvatarUploadPolicy.Instance,
            cancellationToken);
        if (completion.Status == FileUploadCompletionStatus.ObjectChanged)
        {
            return await RejectChangedAsync(upload, command, cancellationToken);
        }
        if (completion.Status != FileUploadCompletionStatus.Success)
        {
            return await RejectAsync(upload, command, completion.FailureReason!, cancellationToken);
        }

        stakeholder.SetAvatarUrl(completion.FinalUrl!);
        upload.MarkCompleted(completion.FinalUrl!, completion.ValidatedETag!);
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

        return new CompleteAvatarUploadResult(CompleteAvatarUploadStatus.Success, completion.FinalUrl);
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
            new DeleteQuarantinedObject(upload.Id, upload.QuarantineObjectKey)
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
