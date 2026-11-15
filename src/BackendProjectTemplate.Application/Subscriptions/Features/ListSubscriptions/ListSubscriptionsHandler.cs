using BackendProjectTemplate.Application.Common.Pagination;
using BackendProjectTemplate.Application.Subscriptions.Administration;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;

namespace BackendProjectTemplate.Application.Subscriptions.Features.ListSubscriptions;

public sealed class ListSubscriptionsHandler(ISubscriptionAdministrationReadModelRepository readModelRepository)
{
    public async Task<ListSubscriptionsResult> HandleAsync(ListSubscriptionsCommand command, CancellationToken cancellationToken)
    {
        var (cursorCreatedAtUtc, cursorSubscriptionId) = CursorPagination.Decode(command.Cursor);
        var page = await readModelRepository.ListSubscriptionsAsync(
            new AdminSubscriptionListRequest(
                command.TenantId,
                command.BillingAccountId,
                command.BillingAccountOwnerType,
                command.StakeholderId,
                command.PlanId,
                command.Status,
                command.AllocationSource,
                command.AutoRenew,
                command.CancelAtPeriodEnd,
                command.CreatedFromUtc,
                command.CreatedToUtc,
                command.CurrentPeriodFromUtc,
                command.CurrentPeriodToUtc,
                cursorCreatedAtUtc,
                cursorSubscriptionId,
                command.Limit),
            cancellationToken);

        var nextCursor = page.HasMore
            ? CursorPagination.Encode(page.Items[^1].CreatedAtUtc, page.Items[^1].SubscriptionId)
            : null;
        return new ListSubscriptionsResult(
            page.Items.Select(AdminSubscriptionResponseMapper.ToResponse).ToArray(),
            nextCursor);
    }
}
