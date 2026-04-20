using BackendProjectTemplate.Application.Authentication.Stakeholders;
using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;

public sealed class RequestPasswordResetHandler(
    IAuthenticationIdentityService identityService,
    ICommandSender commandSender,
    StakeholderResolver stakeholderResolver,
    ICurrentActor currentActor,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<RequestPasswordResetResult> HandleAsync(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentActor.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to request a password reset.");
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await identityService.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return new RequestPasswordResetResult(RequestPasswordResetStatus.UserNotFound);
        }

        var stakeholder = await stakeholderResolver.GetRequiredAsync(user.Id, cancellationToken);

        await commandSender.SendAsync(
            new ResetPasswordCommand
            {
                StakeholderId = stakeholder.Id,
                TenantId = tenantId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetRequested,
            new Dictionary<string, string>
            {
                [Observability.StakeholderIdPropertyName] = stakeholder.Id.ToString(),
                ["Email"] = normalizedEmail
            });

        return new RequestPasswordResetResult(RequestPasswordResetStatus.Success);
    }
}
