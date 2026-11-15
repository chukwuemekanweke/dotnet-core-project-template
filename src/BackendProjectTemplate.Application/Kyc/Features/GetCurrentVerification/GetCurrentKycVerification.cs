using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.Kyc.Features.GetCurrentVerification;

public sealed record GetCurrentKycVerificationQuery(ActorContext ActorContext);

public sealed record GetCurrentKycVerificationResponse(
    Guid? VerificationId,
    string? Provider,
    string Status,
    bool IsVerified,
    string? VerificationUrl,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record GetCurrentKycVerificationResult(bool IsAuthenticated, GetCurrentKycVerificationResponse? Verification = null);

public sealed class GetCurrentKycVerificationHandler(
    IRepository<KycVerification> verificationRepository,
    IRepository<Stakeholder> stakeholderRepository)
{
    public async Task<GetCurrentKycVerificationResult> HandleAsync(GetCurrentKycVerificationQuery query, CancellationToken cancellationToken)
    {
        if (!query.ActorContext.StakeholderId.HasValue || !query.ActorContext.TenantId.HasValue)
        {
            return new(false);
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(query.ActorContext.StakeholderId.Value, cancellationToken);
        var isVerified = stakeholder is not null && stakeholder.TenantId == query.ActorContext.TenantId.Value && stakeholder.IsVerified;
        var current = await verificationRepository.FirstOrDefaultAsync(
            new CurrentKycVerificationSpecification(query.ActorContext.TenantId.Value, query.ActorContext.StakeholderId.Value), cancellationToken);
        return current is null
            ? new(true, new(null, null, KycVerificationStatus.NotStarted.ToString(), isVerified, null, null, null, null))
            : new(true, new(current.Id, current.ProviderKey, current.Status.ToString(), isVerified,
                current.VerificationUrl, current.StartedAtUtc, current.SubmittedAtUtc, current.CompletedAtUtc));
    }
}
