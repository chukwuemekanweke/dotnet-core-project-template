namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

/// <summary>
/// Filters for the administrative KYC summary. Rejections are counted when
/// <c>CompletedAtUtc</c> falls in the half-open interval [<see cref="RejectedFromUtc"/>, <see cref="RejectedToUtc"/>).
/// </summary>
public sealed record AdminKycSummaryFilter(
    Guid? TenantId,
    string? ProviderKey,
    DateTimeOffset RejectedFromUtc,
    DateTimeOffset RejectedToUtc);
