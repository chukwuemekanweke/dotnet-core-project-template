using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Payments;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetBillingHistory;

public sealed record GetBillingHistoryCommand(
    BillingAccountOwnerType BillingAccountOwnerType,
    int Page,
    int PageSize,
    ActorContext ActorContext);
