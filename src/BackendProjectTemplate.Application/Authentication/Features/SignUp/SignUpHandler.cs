using BackendProjectTemplate.Contracts.Events;
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
    IRepository<AppUserStakeholder> appUserStakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const string DefaultStakeholderTypeName = "Customer";
    private const string DefaultStakeholderTypeKey = "customer";

    public async Task<SignUpResult> HandleAsync(SignUpCommand request, CancellationToken cancellationToken)
    {
        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.SignUpRequested);

        if (await identityService.FindByEmailAsync(request.Email) is not null)
        {
            return new SignUpResult(SignUpStatus.DuplicateEmail);
        }

        var now = timeProvider.GetUtcNow();
        var user = AppUser.Create(request.Email, request.FirstName, request.LastName, now);
        var tenantId = currentActor.TenantId ?? Guid.Empty;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var createResult = await identityService.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                return new SignUpResult(SignUpStatus.DuplicateEmail);
            }

            return new SignUpResult(SignUpStatus.ValidationFailed, createResult.ToValidationDictionary());
        }

        var stakeholderType = await stakeholderTypeRepository.FirstOrDefaultAsync(
            new StakeholderTypeByTenantAndKeySpecification(tenantId, DefaultStakeholderTypeKey),
            cancellationToken);
        if (stakeholderType is null)
        {
            stakeholderType = StakeholderType.Create(
                tenantId,
                DefaultStakeholderTypeName,
                DefaultStakeholderTypeKey,
                now);
            await stakeholderTypeRepository.AddAsync(stakeholderType, cancellationToken);
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

        await eventPublisher.PublishAsync(new UserCreated(user.Id, user.Email!)
        {
            OccuredAt = now
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(Observability.EventNames.Authentication.UserCreated, new Dictionary<string, string>
        {
            [Observability.UserIdPropertyName] = user.Id.ToString()
        });

        return new SignUpResult(SignUpStatus.Accepted);
    }
}
