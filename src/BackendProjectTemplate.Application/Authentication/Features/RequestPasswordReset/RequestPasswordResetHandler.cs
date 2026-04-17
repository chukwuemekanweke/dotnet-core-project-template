using BackendProjectTemplate.Contracts.Commands.Authentication;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Application.Authentication.Features.RequestPasswordReset;

public sealed class RequestPasswordResetHandler(
    IAuthenticationIdentityService identityService,
    ICommandSender commandSender,
    IRepository<AppUserStakeholder> appUserStakeholderRepository,
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

        var appUserStakeholder = await appUserStakeholderRepository.FirstOrDefaultAsync(
            new AppUserStakeholderByAppUserIdSpecification(user.Id),
            cancellationToken)
            ?? throw new InvalidOperationException($"Unable to resolve stakeholder for user '{user.Id}' during password reset request.");

        await commandSender.SendAsync(
            new ResetPasswordCommand
            {
                StakeholderId = appUserStakeholder.StakeholderId,
                TenantId = tenantId
            },
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetRequested,
            new Dictionary<string, string>
            {
                [Observability.StakeholderIdPropertyName] = appUserStakeholder.StakeholderId.ToString(),
                ["Email"] = normalizedEmail
            });

        return new RequestPasswordResetResult(RequestPasswordResetStatus.Success);
    }
}
