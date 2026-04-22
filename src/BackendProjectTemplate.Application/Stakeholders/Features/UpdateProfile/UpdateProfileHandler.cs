using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;

public sealed class UpdateProfileHandler(
    ICurrentActor currentActor,
    IRepository<Stakeholder> stakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<UpdateProfileResult> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentActor.ActorId, out var stakeholderId))
        {
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.NotAuthenticated);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.ProfileUpdateFailed,
                ObservabilityEventProperties.Create(currentActor, failureReason: ObservabilityFailureReasons.NotAuthenticated));
            return new UpdateProfileResult(UpdateProfileStatus.NotAuthenticated);
        }

        if (string.IsNullOrWhiteSpace(command.FirstName) || string.IsNullOrWhiteSpace(command.LastName))
        {
            customTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, stakeholderId.ToString());
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.ProfileUpdateFailed,
                ObservabilityEventProperties.Create(currentActor, stakeholderId, ObservabilityFailureReasons.ValidationFailed));
            return new UpdateProfileResult(
                UpdateProfileStatus.ValidationFailed,
                "FirstName and LastName are required.");
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken);
        if (stakeholder is null)
        {
            customTelemetryContext.SetProperty(Observability.StakeholderIdPropertyName, stakeholderId.ToString());
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.StakeholderNotFound);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.ProfileUpdateFailed,
                ObservabilityEventProperties.Create(currentActor, stakeholderId, ObservabilityFailureReasons.StakeholderNotFound));
            return new UpdateProfileResult(UpdateProfileStatus.StakeholderNotFound);
        }

        stakeholder.UpdateProfile(command.FirstName, command.LastName, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.ProfileUpdateCompleted,
            ObservabilityEventProperties.Create(currentActor, stakeholderId));

        return new UpdateProfileResult(UpdateProfileStatus.Success);
    }
}
