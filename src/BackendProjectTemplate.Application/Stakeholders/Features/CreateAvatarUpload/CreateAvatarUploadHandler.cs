using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.Stakeholders.Features.CreateAvatarUpload;

public sealed class CreateAvatarUploadHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<AvatarUpload> avatarUploadRepository,
    IObjectStorageService objectStorageService,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const long MaxAvatarFileSizeBytes = 2 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    public async Task<CreateAvatarUploadResult> HandleAsync(
        CreateAvatarUploadCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.ActorContext.StakeholderId.HasValue || !command.ActorContext.TenantId.HasValue)
        {
            RecordFailure(command, null, ObservabilityFailureReasons.NotAuthenticated);
            return new CreateAvatarUploadResult(CreateAvatarUploadStatus.NotAuthenticated);
        }

        var stakeholderId = command.ActorContext.StakeholderId.Value;
        if (string.IsNullOrWhiteSpace(command.FileName) ||
            command.FileName.Length > 255 ||
            string.IsNullOrWhiteSpace(command.ContentType) ||
            command.ContentLength <= 0 ||
            command.ContentLength > MaxAvatarFileSizeBytes)
        {
            RecordFailure(command, stakeholderId, ObservabilityFailureReasons.InvalidFile);
            return new CreateAvatarUploadResult(
                CreateAvatarUploadStatus.InvalidFile,
                Error: "Avatar must be a JPEG, PNG, or WEBP file with size up to 2 MB.");
        }

        var contentType = command.ContentType.Trim().ToLowerInvariant();
        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
        {
            RecordFailure(command, stakeholderId, ObservabilityFailureReasons.InvalidFile);
            return new CreateAvatarUploadResult(
                CreateAvatarUploadStatus.InvalidFile,
                Error: "Avatar must be a JPEG, PNG, or WEBP file with size up to 2 MB.");
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken);
        if (stakeholder is null || stakeholder.TenantId != command.ActorContext.TenantId.Value)
        {
            RecordFailure(command, stakeholderId, ObservabilityFailureReasons.StakeholderNotFound);
            return new CreateAvatarUploadResult(CreateAvatarUploadStatus.StakeholderNotFound);
        }

        var expiresAtUtc = timeProvider.GetUtcNow().AddMinutes(10);
        var uploadId = Guid.CreateVersion7();
        var quarantineObjectKey =
            $"quarantine/avatars/tenants/{stakeholder.TenantId}/stakeholders/{stakeholder.Id}/{uploadId:N}{extension}";
        var finalObjectKey =
            $"tenants/{stakeholder.TenantId}/stakeholders/{stakeholder.Id}/avatar/{uploadId:N}{extension}";
        var upload = AvatarUpload.Create(
            uploadId,
            stakeholder.Id,
            stakeholder.TenantId,
            command.FileName.Trim(),
            contentType,
            command.ContentLength,
            extension,
            quarantineObjectKey,
            finalObjectKey,
            expiresAtUtc);

        await avatarUploadRepository.AddAsync(upload, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var presignedUpload = await objectStorageService.CreatePrivatePresignedUploadAsync(
            new ObjectStoragePresignedUploadRequest(upload.QuarantineObjectKey, contentType, expiresAtUtc),
            cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadInitiated,
            CreateEventProperties(command, stakeholderId, upload.Id, contentType, command.ContentLength));

        return new CreateAvatarUploadResult(
            CreateAvatarUploadStatus.Success,
            upload.Id,
            presignedUpload.UploadUrl,
            presignedUpload.Headers,
            presignedUpload.ExpiresAtUtc);
    }

    private void RecordFailure(CreateAvatarUploadCommand command, Guid? stakeholderId, string reason)
    {
        customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, reason);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadFailed,
            ObservabilityEventProperties.Create(command.ActorContext, stakeholderId, reason));
    }

    private static Dictionary<string, string> CreateEventProperties(
        CreateAvatarUploadCommand command,
        Guid stakeholderId,
        Guid uploadId,
        string contentType,
        long contentLength) =>
        ObservabilityEventProperties.Create(
            command.ActorContext,
            stakeholderId,
            additionalProperties: new Dictionary<string, string>
            {
                [Observability.PropertyNames.Common.UploadId] = uploadId.ToString(),
                [Observability.PropertyNames.Common.ContentType] = contentType,
                [Observability.PropertyNames.Common.ContentLength] = contentLength.ToString()
            });
}
