using BackendProjectTemplate.Domain.Kyc;
using BackendProjectTemplate.Domain.Kyc.Entities;
using BackendProjectTemplate.Domain.Kyc.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

/// <summary>
/// Bounded, projection-only administrative KYC reporting. Callback data is associated with an attempt
/// only when provider verification/session references resolve uniquely and applicant references agree.
/// </summary>
public sealed class KycAdministrationReadModelRepository(AppReadDbContext dbContext)
    : IKycAdministrationReadModelRepository
{
    public async Task<AdminKycVerificationListPage> ListVerificationsAsync(
        AdminKycVerificationListRequest request,
        CancellationToken cancellationToken)
    {
        var verifications = ApplyFilters(dbContext.KycVerifications.AsNoTracking(), request);

        var query =
            from verification in verifications
            join stakeholder in dbContext.Stakeholders.AsNoTracking()
                on new { verification.TenantId, Id = verification.StakeholderId }
                equals new { stakeholder.TenantId, stakeholder.Id }
            let latestCallback = dbContext.KycWebhookInboxes.AsNoTracking()
                .Where(callback =>
                    callback.ProviderKey == verification.ProviderKey &&
                    ((callback.VerificationReference != null &&
                      callback.VerificationReference == verification.ProviderVerificationReference) ||
                     (callback.SessionReference != null &&
                      callback.SessionReference == verification.ProviderSessionReference)) &&
                    (callback.ExternalApplicantId == null ||
                     callback.ExternalApplicantId == verification.ExternalApplicantId) &&
                    (verification.ProviderApplicantReference == null ||
                     callback.ApplicantReference == verification.ProviderApplicantReference) &&
                    !dbContext.KycVerifications.AsNoTracking().Any(other =>
                        other.Id != verification.Id &&
                        other.ProviderKey == callback.ProviderKey &&
                        ((callback.VerificationReference != null &&
                          callback.VerificationReference == other.ProviderVerificationReference) ||
                         (callback.SessionReference != null &&
                          callback.SessionReference == other.ProviderSessionReference))))
                .OrderByDescending(callback => callback.ReceivedAtUtc)
                .ThenByDescending(callback => callback.Id)
                .Select(callback => new LatestCallbackProjection
                {
                    ProviderStatus = callback.ProviderStatus,
                    ReceivedAtUtc = callback.ReceivedAtUtc
                })
                .FirstOrDefault()
            select new ListProjection
            {
                VerificationId = verification.Id,
                TenantId = verification.TenantId,
                StakeholderId = verification.StakeholderId,
                StakeholderDisplayName = stakeholder.FirstName + " " + stakeholder.LastName,
                ProviderKey = verification.ProviderKey,
                Status = verification.Status,
                LatestProviderStatus = latestCallback == null ? null : latestCallback.ProviderStatus,
                ProviderVerificationReference = verification.ProviderVerificationReference,
                StartedAtUtc = verification.StartedAtUtc,
                SubmittedAtUtc = verification.SubmittedAtUtc,
                CompletedAtUtc = verification.CompletedAtUtc,
                LastReconciledAtUtc = verification.LastReconciledAtUtc
            };

        if (request.ProviderStatus is not null)
        {
            query = query.Where(item => item.LatestProviderStatus == request.ProviderStatus);
        }

        var rows = await query
            .OrderByDescending(item => item.StartedAtUtc)
            .ThenByDescending(item => item.VerificationId)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);
        var hasMore = rows.Count > request.Limit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new AdminKycVerificationListPage(
            rows.Select(item => new AdminKycVerificationListItemReadModel(
                item.VerificationId,
                item.TenantId,
                item.StakeholderId,
                item.StakeholderDisplayName,
                item.ProviderKey,
                item.Status,
                item.LatestProviderStatus,
                item.ProviderVerificationReference,
                item.StartedAtUtc,
                item.SubmittedAtUtc,
                item.CompletedAtUtc,
                item.LastReconciledAtUtc)).ToArray(),
            hasMore);
    }

    public async Task<AdminKycVerificationDetailReadModel?> GetVerificationDetailAsync(
        Guid verificationId,
        int callbackHistoryLimit,
        CancellationToken cancellationToken)
    {
        var detail = await (
                from verification in dbContext.KycVerifications.AsNoTracking()
                join stakeholder in dbContext.Stakeholders.AsNoTracking()
                    on new { verification.TenantId, Id = verification.StakeholderId }
                    equals new { stakeholder.TenantId, stakeholder.Id }
                where verification.Id == verificationId
                select new DetailProjection
                {
                    VerificationId = verification.Id,
                    TenantId = verification.TenantId,
                    StakeholderId = verification.StakeholderId,
                    StakeholderDisplayName = stakeholder.FirstName + " " + stakeholder.LastName,
                    StakeholderIsVerified = stakeholder.IsVerified,
                    ProviderKey = verification.ProviderKey,
                    ExternalApplicantId = verification.ExternalApplicantId,
                    ProviderApplicantReference = verification.ProviderApplicantReference,
                    ProviderVerificationReference = verification.ProviderVerificationReference,
                    ProviderSessionReference = verification.ProviderSessionReference,
                    Status = verification.Status,
                    LastProviderTransition = verification.LastProviderTransition,
                    StartedAtUtc = verification.StartedAtUtc,
                    SubmittedAtUtc = verification.SubmittedAtUtc,
                    CompletedAtUtc = verification.CompletedAtUtc,
                    LastReconciledAtUtc = verification.LastReconciledAtUtc
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (detail is null)
        {
            return null;
        }

        var callbacks = await dbContext.KycWebhookInboxes.AsNoTracking()
            .Where(callback =>
                callback.ProviderKey == detail.ProviderKey &&
                ((callback.VerificationReference != null &&
                  callback.VerificationReference == detail.ProviderVerificationReference) ||
                 (callback.SessionReference != null &&
                  callback.SessionReference == detail.ProviderSessionReference)) &&
                (callback.ExternalApplicantId == null ||
                 callback.ExternalApplicantId == detail.ExternalApplicantId) &&
                (detail.ProviderApplicantReference == null ||
                 callback.ApplicantReference == detail.ProviderApplicantReference) &&
                !dbContext.KycVerifications.AsNoTracking().Any(other =>
                    other.Id != detail.VerificationId &&
                    other.ProviderKey == callback.ProviderKey &&
                    ((callback.VerificationReference != null &&
                      callback.VerificationReference == other.ProviderVerificationReference) ||
                     (callback.SessionReference != null &&
                      callback.SessionReference == other.ProviderSessionReference))))
            .OrderByDescending(callback => callback.ReceivedAtUtc)
            .ThenByDescending(callback => callback.Id)
            .Take(callbackHistoryLimit)
            .Select(callback => new AdminKycCallbackReadModel(
                callback.RequestId,
                callback.EventType,
                callback.ProviderStatus,
                callback.Verified,
                callback.ProcessingOutcome,
                callback.ReceivedAtUtc,
                callback.ProcessedAtUtc))
            .ToListAsync(cancellationToken);

        var latestCallback = callbacks.FirstOrDefault();
        return new AdminKycVerificationDetailReadModel(
            new AdminKycVerificationReadModel(
                detail.VerificationId,
                detail.TenantId,
                detail.StakeholderId,
                detail.ProviderKey,
                detail.Status,
                detail.LastProviderTransition,
                detail.StartedAtUtc,
                detail.SubmittedAtUtc,
                detail.CompletedAtUtc,
                detail.LastReconciledAtUtc),
            new AdminKycStakeholderReadModel(
                detail.StakeholderId,
                detail.StakeholderDisplayName,
                detail.StakeholderIsVerified),
            new AdminKycProviderReadModel(
                detail.ProviderKey,
                detail.ProviderApplicantReference,
                detail.ProviderVerificationReference,
                detail.ProviderSessionReference,
                latestCallback?.ProviderStatus,
                latestCallback?.ReceivedAtUtc),
            callbacks);
    }

    /// <summary>
    /// Single conditional-aggregate query. Visibility matches the list endpoint: soft-deleted attempts
    /// are excluded by the global query filter and attempts whose stakeholder is soft-deleted are
    /// excluded by the inner stakeholder join.
    /// </summary>
    public async Task<AdminKycSummaryReadModel> GetSummaryAsync(
        AdminKycSummaryFilter filter,
        CancellationToken cancellationToken)
    {
        var verifications = dbContext.KycVerifications.AsNoTracking();
        if (filter.TenantId.HasValue)
        {
            verifications = verifications.Where(item => item.TenantId == filter.TenantId.Value);
        }

        if (filter.ProviderKey is not null)
        {
            verifications = verifications.Where(item => item.ProviderKey == filter.ProviderKey);
        }

        var rejectedFromUtc = filter.RejectedFromUtc;
        var rejectedToUtc = filter.RejectedToUtc;
        var totals = await (
                from verification in verifications
                join stakeholder in dbContext.Stakeholders.AsNoTracking()
                    on new { verification.TenantId, Id = verification.StakeholderId }
                    equals new { stakeholder.TenantId, stakeholder.Id }
                where verification.Status == KycVerificationStatus.Pending ||
                      verification.Status == KycVerificationStatus.InProgress ||
                      (verification.Status == KycVerificationStatus.Rejected &&
                       verification.CompletedAtUtc >= rejectedFromUtc &&
                       verification.CompletedAtUtc < rejectedToUtc)
                group verification by 1
                into groupItems
                select new
                {
                    PendingCount = groupItems.LongCount(item => item.Status == KycVerificationStatus.Pending),
                    InProgressCount = groupItems.LongCount(item => item.Status == KycVerificationStatus.InProgress),
                    RejectedCount = groupItems.LongCount(item => item.Status == KycVerificationStatus.Rejected)
                })
            .SingleOrDefaultAsync(cancellationToken);

        return new AdminKycSummaryReadModel(
            totals?.PendingCount ?? 0,
            totals?.InProgressCount ?? 0,
            totals?.RejectedCount ?? 0);
    }

    private IQueryable<KycVerification> ApplyFilters(
        IQueryable<KycVerification> verifications,
        AdminKycVerificationListRequest request)
    {
        if (request.TenantId.HasValue)
        {
            verifications = verifications.Where(item => item.TenantId == request.TenantId.Value);
        }

        if (request.StakeholderId.HasValue)
        {
            verifications = verifications.Where(item => item.StakeholderId == request.StakeholderId.Value);
        }

        if (request.Status.HasValue)
        {
            verifications = verifications.Where(item => item.Status == request.Status.Value);
        }

        if (request.ProviderKey is not null)
        {
            verifications = verifications.Where(item => item.ProviderKey == request.ProviderKey);
        }

        if (request.ProviderReference is not null)
        {
            verifications = verifications.Where(item =>
                item.ProviderVerificationReference == request.ProviderReference ||
                item.ProviderSessionReference == request.ProviderReference);
        }

        if (request.Search is not null)
        {
            var pattern = $"%{EscapeLikePattern(request.Search)}%";
            verifications =
                from verification in verifications
                join stakeholder in dbContext.Stakeholders.AsNoTracking()
                    on new { verification.TenantId, Id = verification.StakeholderId }
                    equals new { stakeholder.TenantId, stakeholder.Id }
                where EF.Functions.ILike(stakeholder.FirstName, pattern, "\\") ||
                      EF.Functions.ILike(stakeholder.LastName, pattern, "\\") ||
                      EF.Functions.ILike(stakeholder.FirstName + " " + stakeholder.LastName, pattern, "\\")
                select verification;
        }

        if (request.StartedFromUtc.HasValue)
        {
            verifications = verifications.Where(item => item.StartedAtUtc >= request.StartedFromUtc.Value);
        }

        if (request.StartedToUtc.HasValue)
        {
            verifications = verifications.Where(item => item.StartedAtUtc < request.StartedToUtc.Value);
        }

        if (request.CursorStartedAtUtc.HasValue && request.CursorVerificationId.HasValue)
        {
            var startedAtUtc = request.CursorStartedAtUtc.Value;
            var verificationId = request.CursorVerificationId.Value;
            verifications = verifications.Where(item =>
                item.StartedAtUtc < startedAtUtc ||
                (item.StartedAtUtc == startedAtUtc && item.Id.CompareTo(verificationId) < 0));
        }

        return verifications;
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed class LatestCallbackProjection
    {
        public string ProviderStatus { get; init; } = string.Empty;
        public DateTimeOffset ReceivedAtUtc { get; init; }
    }

    private sealed class ListProjection
    {
        public Guid VerificationId { get; init; }
        public Guid TenantId { get; init; }
        public Guid StakeholderId { get; init; }
        public string StakeholderDisplayName { get; init; } = string.Empty;
        public string ProviderKey { get; init; } = string.Empty;
        public KycVerificationStatus Status { get; init; }
        public string? LatestProviderStatus { get; init; }
        public string? ProviderVerificationReference { get; init; }
        public DateTimeOffset StartedAtUtc { get; init; }
        public DateTimeOffset? SubmittedAtUtc { get; init; }
        public DateTimeOffset? CompletedAtUtc { get; init; }
        public DateTimeOffset? LastReconciledAtUtc { get; init; }
    }

    private sealed class DetailProjection
    {
        public Guid VerificationId { get; init; }
        public Guid TenantId { get; init; }
        public Guid StakeholderId { get; init; }
        public string StakeholderDisplayName { get; init; } = string.Empty;
        public bool StakeholderIsVerified { get; init; }
        public string ProviderKey { get; init; } = string.Empty;
        public string ExternalApplicantId { get; init; } = string.Empty;
        public string? ProviderApplicantReference { get; init; }
        public string? ProviderVerificationReference { get; init; }
        public string? ProviderSessionReference { get; init; }
        public KycVerificationStatus Status { get; init; }
        public KycVerificationTransition LastProviderTransition { get; init; }
        public DateTimeOffset StartedAtUtc { get; init; }
        public DateTimeOffset? SubmittedAtUtc { get; init; }
        public DateTimeOffset? CompletedAtUtc { get; init; }
        public DateTimeOffset? LastReconciledAtUtc { get; init; }
    }
}
