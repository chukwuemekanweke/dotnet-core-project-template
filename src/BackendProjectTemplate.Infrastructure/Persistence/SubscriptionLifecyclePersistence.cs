using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class SubscriptionLifecyclePersistence(AppDbContext dbContext) : ISubscriptionLifecyclePersistence
{
    public Task AddPeriodAsync(SubscriptionPeriod period, CancellationToken cancellationToken) =>
        dbContext.SubscriptionPeriods.AddAsync(period, cancellationToken).AsTask();

    public Task AddStatusHistoryAsync(SubscriptionStatusHistory history, CancellationToken cancellationToken) =>
        dbContext.SubscriptionStatusHistory.AddAsync(history, cancellationToken).AsTask();
}
