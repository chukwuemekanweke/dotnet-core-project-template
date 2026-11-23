using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using BackendProjectTemplate.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

/// <summary>
/// Bounded, projection-only administrative payment reporting over the read context.
/// Payment status-transition and reconciliation-attempt histories are not persisted, so this repository
/// returns only the current payment state and the latest persisted status-check timestamp.
/// </summary>
public sealed class PaymentAdministrationReadModelRepository(AppReadDbContext dbContext)
    : IPaymentAdministrationReadModelRepository
{
    public async Task<AdminPaymentListPage> ListPaymentsAsync(
        AdminPaymentListRequest request,
        CancellationToken cancellationToken)
    {
        var payments = ApplyFilters(dbContext.PaymentTransactions.AsNoTracking(), request);
        var query = ProjectPayments(payments);
        var rows = await query
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.TransactionId)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);
        var hasMore = rows.Count > request.Limit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new AdminPaymentListPage(
            rows.Select(item => new AdminPaymentListItemReadModel(
                item.TransactionId,
                item.MerchantReference,
                item.ProviderReference,
                item.TenantId,
                item.StakeholderId,
                item.StakeholderDisplayName,
                item.PaymentProviderId,
                item.PaymentProviderName,
                item.PaymentProviderKey,
                item.Amount,
                item.CurrencyId,
                item.CurrencyCode,
                item.PaymentIntent,
                item.PaymentMethodType,
                item.PaymentStatus,
                item.CreatedAtUtc,
                item.CompletedAtUtc,
                item.FailedAtUtc)).ToArray(),
            hasMore);
    }

    public async Task<AdminPaymentDetailReadModel?> GetPaymentDetailAsync(
        Guid paymentTransactionId,
        int webhookHistoryLimit,
        CancellationToken cancellationToken)
    {
        var payment = await ProjectPayments(dbContext.PaymentTransactions.AsNoTracking()
                .Where(item => item.Id == paymentTransactionId))
            .Select(item => new AdminPaymentDetailCoreReadModel(
                item.TransactionId,
                item.MerchantReference,
                item.ProviderReference,
                item.TenantId,
                item.StakeholderId,
                item.StakeholderDisplayName,
                item.InitiatedByUserId,
                item.PaymentProviderId,
                item.PaymentProviderName,
                item.PaymentProviderKey,
                item.Amount,
                item.AmountPaid,
                item.CurrencyId,
                item.CurrencyCode,
                item.PaymentIntent,
                item.PaymentMethodType,
                item.PaymentStatus,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.CompletedAtUtc,
                item.FailedAtUtc,
                item.ExpiresAtUtc,
                item.LastStatusCheckAtUtc,
                item.StatusChangeReason,
                item.FailureReason))
            .SingleOrDefaultAsync(cancellationToken);
        if (payment is null)
        {
            return null;
        }

        var merchantReference = payment.MerchantReference;
        var providerReference = payment.ProviderReference;
        var providerId = payment.PaymentProviderId;
        var webhooks = await dbContext.PaymentWebhookInboxes.AsNoTracking()
            .Where(item => item.PaymentProviderId == providerId &&
                (item.MerchantReference == merchantReference ||
                 (providerReference != null && item.ProviderReference == providerReference)))
            .OrderByDescending(item => item.ReceivedAtUtc)
            .ThenByDescending(item => item.Id)
            .Take(webhookHistoryLimit)
            .Select(item => new AdminPaymentWebhookReadModel(
                item.Id,
                item.WebhookEventName,
                item.WebhookEventId,
                item.WebhookProcessingStatus,
                item.SignatureValidationStatus,
                item.StatusChangeReason,
                item.ReceivedAtUtc,
                item.ProcessedAtUtc))
            .ToListAsync(cancellationToken);

        var fulfillment = await GetFulfillmentAsync(payment, cancellationToken);
        return new AdminPaymentDetailReadModel(payment, webhooks, fulfillment);
    }

    private static IQueryable<PaymentTransaction> ApplyFilters(
        IQueryable<PaymentTransaction> payments,
        AdminPaymentListRequest request)
    {
        if (request.TenantId.HasValue)
        {
            payments = payments.Where(item => item.TenantId == request.TenantId.Value);
        }

        if (request.StakeholderId.HasValue)
        {
            payments = payments.Where(item => item.StakeholderId == request.StakeholderId.Value);
        }

        if (request.PaymentProviderId.HasValue)
        {
            payments = payments.Where(item => item.PaymentProviderId == request.PaymentProviderId.Value);
        }

        if (request.CurrencyId.HasValue)
        {
            payments = payments.Where(item => item.CurrencyId == request.CurrencyId.Value);
        }

        if (request.PaymentIntent.HasValue)
        {
            payments = payments.Where(item => item.PaymentIntent == request.PaymentIntent.Value);
        }

        if (request.PaymentStatus.HasValue)
        {
            payments = payments.Where(item => item.PaymentStatus == request.PaymentStatus.Value);
        }

        if (request.PaymentMethodType.HasValue)
        {
            payments = payments.Where(item => item.PaymentMethodType == request.PaymentMethodType.Value);
        }

        if (request.CreatedFromUtc.HasValue)
        {
            payments = payments.Where(item => item.CreatedAtUtc >= request.CreatedFromUtc.Value);
        }

        if (request.CreatedToUtc.HasValue)
        {
            payments = payments.Where(item => item.CreatedAtUtc < request.CreatedToUtc.Value);
        }

        if (request.Search is not null)
        {
            var pattern = $"%{EscapeLikePattern(request.Search)}%";
            payments = payments.Where(item =>
                EF.Functions.ILike(item.MerchantReference, pattern, "\\") ||
                (item.ProviderReference != null && EF.Functions.ILike(item.ProviderReference, pattern, "\\")));
        }

        if (request.CursorCreatedAtUtc.HasValue && request.CursorPaymentTransactionId.HasValue)
        {
            var cursorCreatedAtUtc = request.CursorCreatedAtUtc.Value;
            var cursorId = request.CursorPaymentTransactionId.Value;
            payments = payments.Where(item =>
                item.CreatedAtUtc < cursorCreatedAtUtc ||
                (item.CreatedAtUtc == cursorCreatedAtUtc && item.Id.CompareTo(cursorId) < 0));
        }

        return payments;
    }

    private IQueryable<PaymentProjection> ProjectPayments(IQueryable<PaymentTransaction> payments) =>
        from payment in payments
        join provider in dbContext.PaymentProviders.AsNoTracking() on payment.PaymentProviderId equals provider.Id
        join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
        join stakeholder in dbContext.Stakeholders.AsNoTracking()
            on new { payment.TenantId, Id = payment.StakeholderId }
            equals new { stakeholder.TenantId, stakeholder.Id } into stakeholders
        from stakeholder in stakeholders.DefaultIfEmpty()
        select new PaymentProjection
        {
            TransactionId = payment.Id,
            MerchantReference = payment.MerchantReference,
            ProviderReference = payment.ProviderReference,
            TenantId = payment.TenantId,
            StakeholderId = payment.StakeholderId,
            StakeholderDisplayName = stakeholder == null ? null : stakeholder.FirstName + " " + stakeholder.LastName,
            InitiatedByUserId = payment.InitiatedByUserId,
            PaymentProviderId = payment.PaymentProviderId,
            PaymentProviderName = provider.ProviderName,
            PaymentProviderKey = provider.ProviderKey,
            Amount = payment.Amount,
            AmountPaid = payment.AmountPaid,
            CurrencyId = payment.CurrencyId,
            CurrencyCode = currency.CurrencyCode,
            PaymentIntent = payment.PaymentIntent,
            PaymentMethodType = payment.PaymentMethodType,
            PaymentStatus = payment.PaymentStatus,
            CreatedAtUtc = payment.CreatedAtUtc,
            UpdatedAtUtc = payment.UpdatedAtUtc,
            CompletedAtUtc = payment.CompletedAtUtc,
            FailedAtUtc = payment.FailedAtUtc,
            ExpiresAtUtc = payment.ExpiresAtUtc,
            LastStatusCheckAtUtc = payment.LastStatusCheckAtUtc,
            StatusChangeReason = payment.StatusChangeReason,
            FailureReason = payment.FailureReason
        };

    private async Task<AdminPaymentFulfillmentReadModel> GetFulfillmentAsync(
        AdminPaymentDetailCoreReadModel payment,
        CancellationToken cancellationToken)
    {
        if (payment.PaymentIntent == PaymentIntent.WalletTopUp)
        {
            var walletCredit = await dbContext.WalletTransactions.AsNoTracking()
                .Where(item => item.PaymentTransactionId == payment.TransactionId &&
                    item.TransactionType == WalletTransactionType.Credit &&
                    item.TransactionCategory == WalletTransactionCategory.WalletFunding)
                .OrderByDescending(item => item.CreatedAtUtc)
                .Select(item => new { item.Id, item.CreatedAtUtc })
                .FirstOrDefaultAsync(cancellationToken);
            return walletCredit is null
                ? new AdminPaymentFulfillmentReadModel("WalletTopUp", "Pending", null, null, null)
                : new AdminPaymentFulfillmentReadModel("WalletTopUp", "Fulfilled", walletCredit.Id, walletCredit.CreatedAtUtc, null);
        }

        var purchase = await dbContext.SubscriptionPurchases.AsNoTracking()
            .Where(item => item.PaymentTransactionId == payment.TransactionId)
            .Select(item => new
            {
                item.SubscriptionId,
                item.FulfilledAtUtc,
                item.FulfillmentBlockedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (purchase is not null)
        {
            var status = purchase.FulfilledAtUtc.HasValue
                ? "Fulfilled"
                : purchase.FulfillmentBlockedAtUtc.HasValue ? "Blocked" : "Pending";
            return new AdminPaymentFulfillmentReadModel(
                "SubscriptionPurchase",
                status,
                purchase.SubscriptionId,
                purchase.FulfilledAtUtc,
                status == "Blocked" ? "Subscription purchase fulfillment is blocked." : null);
        }

        var renewal = await (
                from attempt in dbContext.SubscriptionRenewalAttempts.AsNoTracking()
                join item in dbContext.SubscriptionRenewals.AsNoTracking() on attempt.RenewalId equals item.Id
                where attempt.PaymentTransactionId == payment.TransactionId
                select new { item.Id, item.Status, item.CompletedAtUtc })
            .SingleOrDefaultAsync(cancellationToken);
        if (renewal is not null)
        {
            var status = renewal.Status switch
            {
                SubscriptionRenewalStatus.Fulfilled => "Fulfilled",
                SubscriptionRenewalStatus.FulfillmentBlocked => "Blocked",
                SubscriptionRenewalStatus.Exhausted or SubscriptionRenewalStatus.Cancelled => "NotFulfilled",
                _ => "Pending"
            };
            return new AdminPaymentFulfillmentReadModel(
                "SubscriptionRenewal",
                status,
                renewal.Id,
                renewal.Status == SubscriptionRenewalStatus.Fulfilled ? renewal.CompletedAtUtc : null,
                status == "Blocked" ? "Subscription renewal fulfillment is blocked." : null);
        }

        return new AdminPaymentFulfillmentReadModel("Subscription", "Indeterminate", null, null, null);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed class PaymentProjection
    {
        public Guid TransactionId { get; init; }
        public string MerchantReference { get; init; } = string.Empty;
        public string? ProviderReference { get; init; }
        public Guid TenantId { get; init; }
        public Guid StakeholderId { get; init; }
        public string? StakeholderDisplayName { get; init; }
        public Guid InitiatedByUserId { get; init; }
        public Guid PaymentProviderId { get; init; }
        public string PaymentProviderName { get; init; } = string.Empty;
        public string PaymentProviderKey { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public decimal? AmountPaid { get; init; }
        public Guid CurrencyId { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public PaymentIntent PaymentIntent { get; init; }
        public PaymentMethodType PaymentMethodType { get; init; }
        public PaymentStatus PaymentStatus { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
        public DateTimeOffset UpdatedAtUtc { get; init; }
        public DateTimeOffset? CompletedAtUtc { get; init; }
        public DateTimeOffset? FailedAtUtc { get; init; }
        public DateTimeOffset? ExpiresAtUtc { get; init; }
        public DateTimeOffset? LastStatusCheckAtUtc { get; init; }
        public string? StatusChangeReason { get; init; }
        public string? FailureReason { get; init; }
    }
}
