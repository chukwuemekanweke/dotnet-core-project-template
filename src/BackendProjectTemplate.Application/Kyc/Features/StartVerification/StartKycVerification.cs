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

        var now = timeProvider.GetUtcNow();
        var active = await verificationRepository.FirstOrDefaultAsync(
            new ActiveKycVerificationSpecification(tenantId.Value, stakeholderId.Value), cancellationToken);
        if (active is not null &&
            (!string.IsNullOrWhiteSpace(active.VerificationUrl) || active.Status == KycVerificationStatus.InProgress))
        {
            return Success(active);
        }

        if (active?.HasPendingSessionRequest(now) == true)
        {
            return SessionBeingPrepared();
        }

        var provider = providerServices.Single(service => service.ProviderKey == KycProviderKeys.Kycaid);
        KycVerification verification;
        if (active is null)
        {
            var latest = await verificationRepository.FirstOrDefaultAsync(
                new CurrentKycVerificationSpecification(tenantId.Value, stakeholderId.Value), cancellationToken);
            verification = KycVerification.Start(
                tenantId.Value,
                stakeholderId.Value,
                provider.ProviderKey,
                stakeholderId.Value.ToString("N"),
                latest?.ProviderApplicantReference,
                now);
            verification.MarkSessionRequested(now);
            await verificationRepository.AddAsync(verification, cancellationToken);
        }
        else
        {
            verification = active;
            verification.MarkSessionRequested(now);
        }

        // The session request is committed before calling the provider so that a concurrent or retried start
        // cannot create a second provider form while this request's outcome is unknown.
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is KycActiveVerificationConflictException or KycVerificationConcurrencyException)
        {
            unitOfWork.DiscardChanges();
            var concurrent = await verificationRepository.FirstOrDefaultAsync(
                new ActiveKycVerificationSpecification(tenantId.Value, stakeholderId.Value), cancellationToken);
            return concurrent is not null && !string.IsNullOrWhiteSpace(concurrent.VerificationUrl)
                ? Success(concurrent)
                : SessionBeingPrepared();
        }

        KycSessionResult session;
        try
        {
            session = await provider.CreateSessionAsync(
                new KycSessionRequest(verification.ExternalApplicantId, verification.ProviderApplicantReference), cancellationToken);
        }
        catch (KycProviderRequestRejectedException exception)
        {
            // The provider definitively did not create a form, so the request can be retried immediately.
            verification.ReleaseSessionRequest();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new(StartKycVerificationStatus.ProviderUnavailable, Error: exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return UnknownSessionOutcome(exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return UnknownSessionOutcome(exception);
        }
        catch (InvalidOperationException exception)
        {
            return UnknownSessionOutcome(exception);
        }

        verification.AttachSession(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(verification);
    }

    // The provider may or may not have created a form. The session request stays pending until
    // KycVerification.SessionProvisioningLease elapses, after which a new start may request a fresh session.
    private static StartKycVerificationResult UnknownSessionOutcome(Exception exception) =>
        new(StartKycVerificationStatus.ProviderUnavailable, Error: exception.Message);

    private static StartKycVerificationResult SessionBeingPrepared() =>
        new(
            StartKycVerificationStatus.ProviderUnavailable,
            Error: "A verification session is already being prepared. Try again shortly.");

    private static StartKycVerificationResult Success(KycVerification verification) =>
        new(StartKycVerificationStatus.Success, new(verification.Id, verification.ProviderKey, verification.Status.ToString(), verification.VerificationUrl));
}
