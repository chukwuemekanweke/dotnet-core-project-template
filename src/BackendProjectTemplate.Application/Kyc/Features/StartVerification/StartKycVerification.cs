using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Specifications;

namespace BackendProjectTemplate.Application.Kyc.Features.StartVerification;

public sealed record StartKycVerificationCommand(ActorContext ActorContext);

public sealed record StartKycVerificationResponse(Guid VerificationId, string Provider, string Status, string? VerificationUrl);

public sealed record StartKycVerificationResult(StartKycVerificationStatus Status, StartKycVerificationResponse? Verification = null, string? Error = null);

public enum StartKycVerificationStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3,
    AlreadyVerified = 4,
    IncompleteProfile = 5,
    ProviderUnavailable = 6
}

public sealed class StartKycVerificationHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<KycVerification> verificationRepository,
    IEnumerable<IKycProviderService> providerServices,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<StartKycVerificationResult> HandleAsync(StartKycVerificationCommand command, CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId;
        var tenantId = command.ActorContext.TenantId;
        if (!stakeholderId.HasValue || !tenantId.HasValue)
        {
            return new(StartKycVerificationStatus.NotAuthenticated);
        }

        var stakeholder = await stakeholderRepository.FirstOrDefaultAsync(
            new StakeholderByTenantAndIdSpecification(tenantId.Value, stakeholderId.Value), cancellationToken);
        if (stakeholder is null)
        {
            return new(StartKycVerificationStatus.StakeholderNotFound);
        }

        if (stakeholder.IsVerified)
        {
            return new(StartKycVerificationStatus.AlreadyVerified);
        }

        if (string.IsNullOrWhiteSpace(stakeholder.FirstName) || string.IsNullOrWhiteSpace(stakeholder.LastName))
        {
            return new(StartKycVerificationStatus.IncompleteProfile, Error: "First and last name are required before verification can start.");
        }

        var active = await verificationRepository.FirstOrDefaultAsync(
            new ActiveKycVerificationSpecification(tenantId.Value, stakeholderId.Value), cancellationToken);
        if (active is not null && !string.IsNullOrWhiteSpace(active.VerificationUrl))
        {
            return Success(active);
        }

        var provider = providerServices.Single(service => service.ProviderKey == KycProviderKeys.Kycaid);
        var latest = active ?? await verificationRepository.FirstOrDefaultAsync(
            new CurrentKycVerificationSpecification(tenantId.Value, stakeholderId.Value), cancellationToken);
        var verification = active ?? KycVerification.Start(
            tenantId.Value,
            stakeholderId.Value,
            provider.ProviderKey,
            stakeholderId.Value.ToString("N"),
            latest?.ProviderApplicantReference,
            timeProvider.GetUtcNow());

        if (active is null)
        {
            await verificationRepository.AddAsync(verification, cancellationToken);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (KycActiveVerificationConflictException)
            {
                unitOfWork.DiscardChanges();
                var concurrent = await verificationRepository.FirstOrDefaultAsync(
                    new ActiveKycVerificationSpecification(tenantId.Value, stakeholderId.Value), cancellationToken);
                return concurrent is null
                    ? new(StartKycVerificationStatus.ProviderUnavailable)
                    : Success(concurrent);
            }
        }

        try
        {
            var session = await provider.CreateSessionAsync(
                new KycSessionRequest(verification.ExternalApplicantId, verification.ProviderApplicantReference), cancellationToken);
            verification.AttachSession(session);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Success(verification);
        }
        catch (HttpRequestException exception)
        {
            verification.MarkFailed(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(StartKycVerificationStatus.ProviderUnavailable, Error: exception.Message);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            verification.MarkFailed(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(StartKycVerificationStatus.ProviderUnavailable, Error: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            verification.MarkFailed(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(StartKycVerificationStatus.ProviderUnavailable, Error: exception.Message);
        }
    }

    private static StartKycVerificationResult Success(KycVerification verification) =>
        new(StartKycVerificationStatus.Success, new(verification.Id, verification.ProviderKey, verification.Status.ToString(), verification.VerificationUrl));
}
