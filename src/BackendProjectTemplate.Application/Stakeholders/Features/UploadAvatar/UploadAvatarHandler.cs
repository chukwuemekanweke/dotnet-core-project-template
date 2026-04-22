using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;

public sealed class UploadAvatarHandler(
    ICurrentActor currentActor,
    IRepository<Stakeholder> stakeholderRepository,
    IObjectStorageService objectStorageService,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const long MaxAvatarFileSizeBytes = 2 * 1024 * 1024;

    public async Task<UploadAvatarResult> HandleAsync(UploadAvatarCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentActor.ActorId, out var stakeholderId))
        {
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.NotAuthenticated);
            return new UploadAvatarResult(UploadAvatarStatus.NotAuthenticated);
        }

        if (command.ContentLength <= 0 ||
            command.ContentLength > MaxAvatarFileSizeBytes ||
            string.IsNullOrWhiteSpace(command.ContentType) ||
            !command.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            customTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, stakeholderId.ToString());
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.InvalidFile);
            return new UploadAvatarResult(
                UploadAvatarStatus.InvalidFile,
                Error: "Avatar must be an image file with size up to 2 MB.");
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken);
        if (stakeholder is null)
        {
            customTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, stakeholderId.ToString());
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.StakeholderNotFound);
            return new UploadAvatarResult(UploadAvatarStatus.StakeholderNotFound);
        }

        var fileExtension = Path.GetExtension(command.FileName);
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            fileExtension = ".bin";
        }

        var objectKey =
            $"tenants/{stakeholder.TenantId}/stakeholders/{stakeholder.Id}/avatar/{Guid.CreateVersion7():N}{fileExtension.ToLowerInvariant()}";
        var avatarUrl = await objectStorageService.UploadPublicAsync(
            new ObjectStorageUploadRequest(objectKey, command.Content, command.ContentType),
            cancellationToken);

        stakeholder.SetAvatarUrl(avatarUrl, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadCompleted,
            ObservabilityEventProperties.Create(currentActor, stakeholderId));

        return new UploadAvatarResult(UploadAvatarStatus.Success, avatarUrl);
    }
}
