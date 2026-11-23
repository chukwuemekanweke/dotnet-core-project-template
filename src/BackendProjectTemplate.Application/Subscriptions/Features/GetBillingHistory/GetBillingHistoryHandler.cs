using BackendProjectTemplate.Domain.Subscriptions.ReadModels;

namespace BackendProjectTemplate.Application.Subscriptions.Features.GetBillingHistory;

public sealed class GetBillingHistoryHandler(ICustomerSubscriptionReadModelRepository readModelRepository)
{
    public async Task<GetBillingHistoryResponse> HandleAsync(
        GetBillingHistoryCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = command.ActorContext.TenantId
            ?? throw new InvalidOperationException("Authenticated tenant id is required.");
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required.");
        var page = await readModelRepository.GetBillingHistoryAsync(
            new CustomerBillingHistoryRequest(
                tenantId,
                command.BillingAccountOwnerType,
                stakeholderId,
                command.Page,
                command.PageSize),
            cancellationToken);
        return new GetBillingHistoryResponse(
            page.Items.Select(item => new BillingHistoryItemResponse(
                item.PaymentTransactionId,
                item.SubscriptionId,
                item.PlanCode,
                item.PlanName,
                item.BillingPeriodStartUtc,
                item.BillingPeriodEndUtc,
                item.Amount,
                item.CurrencyCode,
                item.PaymentStatus.ToString(),
                item.PaymentProviderName,
                item.PaidAtUtc,
                item.MerchantReference,
                item.CreatedAtUtc)).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}
