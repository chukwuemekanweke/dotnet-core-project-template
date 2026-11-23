using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

public sealed class CustomerSubscriptionReadModelRepository(AppReadDbContext dbContext)
    : ICustomerSubscriptionReadModelRepository
{
    public Task<CustomerSubscriptionPurchaseStatusReadModel?> GetPurchaseStatusAsync(
        CustomerSubscriptionPurchaseStatusRequest request,
        CancellationToken cancellationToken) =>
        (
            from purchase in dbContext.SubscriptionPurchases.AsNoTracking()
            join account in dbContext.BillingAccounts.AsNoTracking()
                on new { purchase.TenantId, Id = purchase.BillingAccountId }
                equals new { account.TenantId, account.Id }
            join subscription in dbContext.Subscriptions.AsNoTracking() on purchase.SubscriptionId equals subscription.Id
            join payment in dbContext.PaymentTransactions.AsNoTracking() on purchase.PaymentTransactionId equals payment.Id
            join provider in dbContext.PaymentProviders.AsNoTracking() on payment.PaymentProviderId equals provider.Id
            join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
            where purchase.Id == request.PurchaseId &&
                purchase.TenantId == request.TenantId &&
                account.OwnerType == request.BillingAccountOwnerType &&
                (account.OwnerType == BillingAccountOwnerType.Tenant || account.StakeholderId == request.StakeholderId)
            select new CustomerSubscriptionPurchaseStatusReadModel(
                purchase.Id,
                subscription.Id,
                payment.MerchantReference,
                payment.PaymentStatus,
                payment.PaymentMethodType,
                provider.ProviderName,
                payment.Amount,
                currency.CurrencyCode,
                payment.ExpiresAtUtc,
                payment.CompletedAtUtc,
                purchase.FulfilledAtUtc,
                purchase.FulfillmentBlockedAtUtc,
                purchase.FulfillmentBlockedReason,
                subscription.Status,
                subscription.CurrentPeriodEndUtc,
                purchase.CreatedAtUtc,
                purchase.UpdatedAtUtc))
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<CustomerBillingHistoryPage> GetBillingHistoryAsync(
        CustomerBillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var purchases =
            from purchase in dbContext.SubscriptionPurchases.AsNoTracking()
            join account in dbContext.BillingAccounts.AsNoTracking()
                on new { purchase.TenantId, Id = purchase.BillingAccountId }
                equals new { account.TenantId, account.Id }
            join subscription in dbContext.Subscriptions.AsNoTracking() on purchase.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans.AsNoTracking() on purchase.PlanId equals plan.Id
            join payment in dbContext.PaymentTransactions.AsNoTracking() on purchase.PaymentTransactionId equals payment.Id
            join provider in dbContext.PaymentProviders.AsNoTracking() on payment.PaymentProviderId equals provider.Id
            join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
            join period in dbContext.SubscriptionPeriods.AsNoTracking()
                on payment.Id equals period.PaymentTransactionId into periodGroup
            from period in periodGroup.DefaultIfEmpty()
            where purchase.TenantId == request.TenantId &&
                account.OwnerType == request.BillingAccountOwnerType &&
                (account.OwnerType == BillingAccountOwnerType.Tenant || account.StakeholderId == request.StakeholderId)
            select new CustomerBillingHistoryProjection
            {
                PaymentTransactionId = payment.Id,
                SubscriptionId = subscription.Id,
                PlanCode = purchase.PlanCode,
                PlanName = plan.Name,
                BillingPeriodStartUtc = period == null ? null : period.PeriodStartUtc,
                BillingPeriodEndUtc = period == null ? null : period.PeriodEndUtc,
                Amount = payment.Amount,
                CurrencyCode = currency.CurrencyCode,
                PaymentStatus = payment.PaymentStatus,
                PaymentProviderName = provider.ProviderName,
                PaidAtUtc = payment.CompletedAtUtc,
                MerchantReference = payment.MerchantReference,
                CreatedAtUtc = payment.CreatedAtUtc
            };

        var renewals =
            from attempt in dbContext.SubscriptionRenewalAttempts.AsNoTracking()
            join renewal in dbContext.SubscriptionRenewals.AsNoTracking() on attempt.RenewalId equals renewal.Id
            join account in dbContext.BillingAccounts.AsNoTracking()
                on new { renewal.TenantId, Id = renewal.BillingAccountId }
                equals new { account.TenantId, account.Id }
            join subscription in dbContext.Subscriptions.AsNoTracking() on renewal.SubscriptionId equals subscription.Id
            join plan in dbContext.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
            join payment in dbContext.PaymentTransactions.AsNoTracking() on attempt.PaymentTransactionId equals payment.Id
            join provider in dbContext.PaymentProviders.AsNoTracking() on payment.PaymentProviderId equals provider.Id
            join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
            join period in dbContext.SubscriptionPeriods.AsNoTracking()
                on payment.Id equals period.PaymentTransactionId into periodGroup
            from period in periodGroup.DefaultIfEmpty()
            where renewal.TenantId == request.TenantId &&
                account.OwnerType == request.BillingAccountOwnerType &&
                (account.OwnerType == BillingAccountOwnerType.Tenant || account.StakeholderId == request.StakeholderId)
            select new CustomerBillingHistoryProjection
            {
                PaymentTransactionId = payment.Id,
                SubscriptionId = subscription.Id,
                PlanCode = plan.Code,
                PlanName = plan.Name,
                BillingPeriodStartUtc = period == null ? renewal.TargetPeriodStartUtc : period.PeriodStartUtc,
                BillingPeriodEndUtc = period == null ? renewal.TargetPeriodEndUtc : period.PeriodEndUtc,
                Amount = payment.Amount,
                CurrencyCode = currency.CurrencyCode,
                PaymentStatus = payment.PaymentStatus,
                PaymentProviderName = provider.ProviderName,
                PaidAtUtc = payment.CompletedAtUtc,
                MerchantReference = payment.MerchantReference,
                CreatedAtUtc = payment.CreatedAtUtc
            };

        var query = purchases.Concat(renewals);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.PaymentTransactionId)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new CustomerBillingHistoryItemReadModel(
                item.PaymentTransactionId,
                item.SubscriptionId,
                item.PlanCode,
                item.PlanName,
                item.BillingPeriodStartUtc,
                item.BillingPeriodEndUtc,
                item.Amount,
                item.CurrencyCode,
                item.PaymentStatus,
                item.PaymentProviderName,
                item.PaidAtUtc,
                item.MerchantReference,
                item.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new CustomerBillingHistoryPage(items, request.Page, request.PageSize, totalCount);
    }

    private sealed class CustomerBillingHistoryProjection
    {
        public Guid PaymentTransactionId { get; init; }
        public Guid SubscriptionId { get; init; }
        public string PlanCode { get; init; } = string.Empty;
        public string PlanName { get; init; } = string.Empty;
        public DateTimeOffset? BillingPeriodStartUtc { get; init; }
        public DateTimeOffset? BillingPeriodEndUtc { get; init; }
        public decimal Amount { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public Contracts.Payments.PaymentStatus PaymentStatus { get; init; }
        public string PaymentProviderName { get; init; } = string.Empty;
        public DateTimeOffset? PaidAtUtc { get; init; }
        public string MerchantReference { get; init; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}
