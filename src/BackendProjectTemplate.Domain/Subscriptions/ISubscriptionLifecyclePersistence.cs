using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Domain.Subscriptions;

public interface ISubscriptionLifecyclePersistence
{
    Task AddPeriodAsync(SubscriptionPeriod period, CancellationToken cancellationToken);
    Task AddStatusHistoryAsync(SubscriptionStatusHistory history, CancellationToken cancellationToken);
}
