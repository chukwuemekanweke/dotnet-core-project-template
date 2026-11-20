namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public interface IActiveUserAnalyticsReadModelRepository
{
    Task<ActiveUserAnalyticsSummary> GetSummaryAsync(
        DateOnly date,
        Guid? tenantId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ActiveUserAnalyticsDailyRow>> GetTimelineAsync(
        DateOnly from,
        DateOnly to,
        Guid? tenantId,
        CancellationToken cancellationToken);
    Task<ActiveUserListPage> ListAsync(
        ActiveUserReadModelListRequest request,
        CancellationToken cancellationToken);
}
