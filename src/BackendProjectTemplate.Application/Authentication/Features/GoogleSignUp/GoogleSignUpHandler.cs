using BackendProjectTemplate.Application.Authentication.Constants;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Authentication;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignUp;

public sealed class GoogleSignUpHandler(
    IAuthenticationIdentityService identityService,
    IGoogleIdentityTokenService googleIdentityTokenService,
    IEventPublisher eventPublisher,
    ICurrentActor currentActor,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Stakeholder> stakeholderRepository,
    IAppUserStakeholderRepository appUserStakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<GoogleSignUpResult> HandleAsync(GoogleSignUpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.SignUpRequested);

        var googleIdentity = await googleIdentityTokenService.ValidateAsync(request.IdToken, cancellationToken);
        if (googleIdentity is null)
        {
            return new GoogleSignUpResult(GoogleSignUpStatus.InvalidGoogleToken);
        }

        if (await identityService.FindByEmailAsync(googleIdentity.Email) is not null)
        {
            return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateEmail);
        }

        var now = timeProvider.GetUtcNow();
        var user = AppUser.Create(googleIdentity.Email, request.FirstName, request.LastName, now);
        user.MarkEmailVerified(now);

        var tenantId = currentActor.TenantId
            ?? throw new InvalidOperationException("Tenant id is required to sign up.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateEmail);
            }

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
                return new GoogleSignUpResult(GoogleSignUpStatus.DuplicateGoogleAccount);
            }

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
            tenantId,
            request.CountryId,
            stakeholderType.Id,
            request.FirstName,
            request.LastName,
            now);
        await stakeholderRepository.AddAsync(stakeholder, cancellationToken);

        var appUserStakeholder = AppUserStakeholder.Create(user.Id, stakeholder.Id, now);
        await appUserStakeholderRepository.AddAsync(appUserStakeholder, cancellationToken);

        await eventPublisher.PublishAsync(new UserCreated
        {
            StakeholderId = stakeholder.Id,
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserCreated, new Dictionary<string, string>
        {
            [Observability.StakeholderIdPropertyName] = stakeholder.Id.ToString()
        });

        return new GoogleSignUpResult(GoogleSignUpStatus.Accepted, googleIdentity.Email);
    }
}
