namespace BackendProjectTemplate.Domain.Subscriptions.ReadModels;

public interface ICustomerSubscriptionReadModelRepository
{
    Task<CustomerSubscriptionPurchaseStatusReadModel?> GetPurchaseStatusAsync(
        CustomerSubscriptionPurchaseStatusRequest request,
        CancellationToken cancellationToken);

    Task<CustomerBillingHistoryPage> GetBillingHistoryAsync(
        CustomerBillingHistoryRequest request,
        CancellationToken cancellationToken);
}
