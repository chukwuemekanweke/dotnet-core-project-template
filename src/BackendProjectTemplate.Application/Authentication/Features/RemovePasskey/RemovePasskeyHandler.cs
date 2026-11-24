using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.RemovePasskey;

public sealed class RemovePasskeyHandler(
    IAuthenticationIdentityService identityService,
    IPasskeyService passkeyService,
    StakeholderResolver stakeholderResolver,
    ICustomTelemetryContext telemetryContext,
    ICurrentActor currentActor,
    IEventPublisher eventPublisher,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<PasskeyOperationStatus> HandleAsync(
        RemovePasskeyCommand command,
        CancellationToken cancellationToken)
    {
        var user = await identityService.FindByIdAsync(command.AppUserId);
        if (user is null || user.IsDeleted)
            return PasskeyOperationStatus.NotFound;
        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);
        if (stakeholder.Id != command.StakeholderId || stakeholder.TenantId != command.TenantId)
            return PasskeyOperationStatus.NotFound;
        var result = await passkeyService.RemoveAsync(user, command.CredentialId);
        if (result == PasskeyOperationStatus.Success)
        {
            var actorContext = ActorContext.FromCurrentActor(currentActor);
            await eventPublisher.PublishAsync(
                new UserSecurityActivityOccurred(UserSecurityActivityTypes.PasskeyRemoved, command.IpAddress, command.UserAgent)
                {
                    StakeholderId = stakeholder.Id,
                    FlowId = actorContext.FlowId,
                    OccuredAt = timeProvider.GetUtcNow()
                },
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            telemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.PasskeyRemoved,
                ObservabilityEventProperties.Create(actorContext, stakeholder.Id));
        }
        return result;
    }
}
