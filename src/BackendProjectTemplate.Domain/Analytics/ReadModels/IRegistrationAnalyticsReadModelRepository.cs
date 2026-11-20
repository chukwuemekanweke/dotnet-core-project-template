using BackendProjectTemplate.Domain.Common.Analytics;

namespace BackendProjectTemplate.Domain.Analytics.ReadModels;

public interface IRegistrationAnalyticsReadModelRepository
{
    Task<RegistrationAnalyticsCounts> GetCountsAsync(
        RegistrationAnalyticsFilter filter,
        DateTimeOffset previousFromUtc,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<RegistrationAnalyticsTimelineRow>> GetTimelineAsync(
        RegistrationAnalyticsFilter filter,
        Interval interval,
        CancellationToken cancellationToken);
    Task<RegisteredUserListPage> ListAsync(
        RegisteredUserListRequest request,
        CancellationToken cancellationToken);
}
