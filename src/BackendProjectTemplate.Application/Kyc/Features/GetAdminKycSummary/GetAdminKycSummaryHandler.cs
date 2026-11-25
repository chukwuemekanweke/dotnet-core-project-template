using BackendProjectTemplate.Domain.Kyc.ReadModels;

namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;

/// <summary>
/// Counts current Pending and InProgress KYC attempts and attempts rejected in the rolling seven-day
/// window [asOfUtc - 7 days, asOfUtc). Counts are attempts, not distinct stakeholders.
/// </summary>
public sealed class GetAdminKycSummaryHandler(
    IKycAdministrationReadModelRepository readModelRepository,
    TimeProvider timeProvider)
{
    public static readonly TimeSpan RejectionWindow = TimeSpan.FromDays(7);

    public async Task<GetAdminKycSummaryResponse> HandleAsync(
        GetAdminKycSummaryCommand command,
        CancellationToken cancellationToken)
    {
        var asOfUtc = timeProvider.GetUtcNow().ToUniversalTime();
        var rejectedFromUtc = asOfUtc - RejectionWindow;
        var summary = await readModelRepository.GetSummaryAsync(
            new AdminKycSummaryFilter(
                command.TenantId,
                string.IsNullOrWhiteSpace(command.ProviderKey) ? null : command.ProviderKey.Trim(),
                rejectedFromUtc,
                asOfUtc),
            cancellationToken);

        return new GetAdminKycSummaryResponse(
            summary.PendingCount,
            summary.InProgressCount,
            summary.RejectedCount,
            asOfUtc,
            rejectedFromUtc);
    }
}
