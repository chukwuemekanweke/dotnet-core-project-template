using BackendProjectTemplate.Application.Authentication.Constants;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;

public sealed class GoogleSignUpHandler(
    IAuthenticationIdentityService identityService,
    IGoogleIdentityTokenService googleIdentityTokenService,
    IEventPublisher eventPublisher,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Stakeholder> stakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GoogleSignUpResult> HandleAsync(GoogleSignUpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignUpStarted,
            ObservabilityEventProperties.Create(request.ActorContext));

        var googleIdentity = await googleIdentityTokenService.ValidateAsync(request.IdToken, cancellationToken);
        if (googleIdentity is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidGoogleToken);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.InvalidGoogleToken));
            return new GoogleSignUpResult(GoogleSignUpStatus.InvalidGoogleToken);
        }

        if (await identityService.FindByEmailAsync(googleIdentity.Email) is not null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
            return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateEmail);
        }

        var now = timeProvider.GetUtcNow();
        var user = AppUser.Create(googleIdentity.Email, request.FirstName, request.LastName, now);
        user.MarkEmailVerified(now);

        var tenantId = request.ActorContext.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to sign up.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateEmail);
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Authentication.GoogleSignUpFailed,
                    ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateEmail));
                return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateEmail);
            }

            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new GoogleSignUpResult(GoogleSignUpStatus.ValidationFailed, ValidationErrors: createResult.ToValidationDictionary());
        }

        var addLoginResult = await identityService.AddLoginAsync(
            user,
            ExternalLoginProviders.Google,
            googleIdentity.Subject,
            ExternalLoginProviders.Google);
        if (!addLoginResult.Succeeded)
        {
            if (addLoginResult.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.LoginAlreadyAssociated)))
            {
                customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.DuplicateGoogleAccount);
                customTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Authentication.GoogleSignUpFailed,
                    ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.DuplicateGoogleAccount));
                return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateGoogleAccount);
            }

            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.GoogleSignUpFailed,
                ObservabilityEventProperties.Create(request.ActorContext, failureReason: ObservabilityFailureReasons.ValidationFailed));
            return new GoogleSignUpResult(GoogleSignUpStatus.ValidationFailed, ValidationErrors: addLoginResult.ToValidationDictionary());
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
            FlowId = request.ActorContext.FlowId,
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.GoogleSignUpCompleted,
            ObservabilityEventProperties.Create(request.ActorContext, stakeholder.Id));

        return new GoogleSignUpResult(GoogleSignUpStatus.Accepted, googleIdentity.Email);
    }
}
