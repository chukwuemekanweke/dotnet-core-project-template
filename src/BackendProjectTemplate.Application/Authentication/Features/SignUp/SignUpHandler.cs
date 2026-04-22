using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Application.Authentication.Constants;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Application.Authentication.Features.SignUp;

public sealed class SignUpHandler(
    IAuthenticationIdentityService identityService,
    IEventPublisher eventPublisher,
    ICurrentActor currentActor,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Stakeholder> stakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SignUpResult> HandleAsync(SignUpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignUpStarted,
            ObservabilityEventProperties.Create(currentActor));

        if (await identityService.FindByEmailAsync(request.Email) is not null)
        {
            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.DuplicateEmail);
            return new SignUpResult(SignUpStatus.DuplicateEmail);
        }

        var now = timeProvider.GetUtcNow();
        var user = AppUser.Create(request.Email, request.FirstName, request.LastName, now);
        var tenantId = currentActor.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to sign up.");
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.DuplicateEmail);
                return new SignUpResult(SignUpStatus.DuplicateEmail);
            }

            customTelemetryContext.SetProperty(Observability.FailureReasonPropertyName, ObservabilityFailureReasons.ValidationFailed);
            return new SignUpResult(SignUpStatus.ValidationFailed, createResult.ToValidationDictionary());
        }

        var stakeholderType = await stakeholderTypeRepository.FirstOrDefaultAsync(
            new StakeholderTypeByTenantAndKeySpecification(tenantId, StakeholderDefaults.TypeKey),
            cancellationToken);
        if (stakeholderType is null)
        {
            throw new InvalidOperationException(
                $"Stakeholder type '{StakeholderDefaults.TypeKey}' is not configured for tenant '{tenantId}'.");
        }

        var stakeholder = Stakeholder.Create(
            user.Id,
            tenantId,
            request.CountryId,
            stakeholderType.Id,
            request.FirstName,
            request.LastName,
            now);
        await stakeholderRepository.AddAsync(stakeholder, cancellationToken);

        await eventPublisher.PublishAsync(new UserCreated
        {
            StakeholderId = stakeholder.Id,
            FlowId = currentActor.FlowId,
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordSignUpCompleted,
            ObservabilityEventProperties.Create(currentActor, stakeholder.Id));

        return new SignUpResult(SignUpStatus.Accepted);
    }
}
