namespace BackendProjectTemplate.Application.Subscriptions.Features.GetSubscriptionDetail;

public sealed record GetSubscriptionDetailCommand(Guid SubscriptionId, int HistoryLimit);
