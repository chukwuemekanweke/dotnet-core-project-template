namespace BackendProjectTemplate.Application.Kyc.Features.GetAdminKycSummary;

public sealed record GetAdminKycSummaryResponse(
    long PendingCount,
    long InProgressCount,
    long RejectedLastSevenDaysCount,
    DateTimeOffset AsOfUtc,
    DateTimeOffset RejectedFromUtc);
