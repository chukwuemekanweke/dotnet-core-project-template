namespace BackendProjectTemplate.Domain.Kyc.ReadModels;

public sealed record AdminKycSummaryReadModel(
    long PendingCount,
    long InProgressCount,
    long RejectedCount);
