using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace BackendProjectTemplate.Infrastructure.Persistence;

/// <summary>
/// Projection-only administrative read paths over the read context. Listing pages by
/// <c>(CreatedAtUtc, Id)</c> cursor and never materializes aggregates; detail issues a fixed number of
/// bounded queries (no per-row lookups) regardless of history size.
/// </summary>
public sealed class SubscriptionAdministrationReadModelRepository(AppReadDbContext dbContext)
    : ISubscriptionAdministrationReadModelRepository
{
    public async Task<AdminSubscriptionListPage> ListSubscriptionsAsync(
        AdminSubscriptionListRequest request,
        CancellationToken cancellationToken)
    {
        var subscriptions = dbContext.Subscriptions.AsNoTracking();
        if (request.TenantId.HasValue)
        {
            subscriptions = subscriptions.Where(subscription => subscription.TenantId == request.TenantId.Value);
        }

        if (request.BillingAccountId.HasValue)
        {
            subscriptions = subscriptions.Where(subscription => subscription.BillingAccountId == request.BillingAccountId.Value);
        }

        if (request.PlanId.HasValue)
        {
            subscriptions = subscriptions.Where(subscription => subscription.PlanId == request.PlanId.Value);
        }

        if (request.Status.HasValue)
        {
            subscriptions = subscriptions.Where(subscription => subscription.Status == request.Status.Value);
        }

        if (request.AutoRenew.HasValue)
        {
            subscriptions = subscriptions.Where(subscription => subscription.AutoRenew == request.AutoRenew.Value);
        }

        if (request.CancelAtPeriodEnd.HasValue)
        {
            subscriptions = subscriptions.Where(subscription => subscription.CancelAtPeriodEnd == request.CancelAtPeriodEnd.Value);
        }

        if (request.CreatedFromUtc.HasValue)
        {
            subscriptions = subscriptions.Where(subscription => subscription.CreatedAtUtc >= request.CreatedFromUtc.Value);
        }

        if (request.CreatedToUtc.HasValue)
        {
            subscriptions = subscriptions.Where(subscription => subscription.CreatedAtUtc < request.CreatedToUtc.Value);
        }

        if (request.CurrentPeriodFromUtc.HasValue)
        {
            subscriptions = subscriptions.Where(subscription =>
                subscription.CurrentPeriodEndUtc != null && subscription.CurrentPeriodEndUtc > request.CurrentPeriodFromUtc.Value);
        }

        if (request.CurrentPeriodToUtc.HasValue)
        {
            subscriptions = subscriptions.Where(subscription =>
                subscription.CurrentPeriodStartUtc != null && subscription.CurrentPeriodStartUtc < request.CurrentPeriodToUtc.Value);
        }

        if (request.CursorCreatedAtUtc.HasValue && request.CursorSubscriptionId.HasValue)
        {
            var cursorCreatedAtUtc = request.CursorCreatedAtUtc.Value;
            var cursorSubscriptionId = request.CursorSubscriptionId.Value;
            subscriptions = subscriptions.Where(subscription =>
                subscription.CreatedAtUtc < cursorCreatedAtUtc ||
                (subscription.CreatedAtUtc == cursorCreatedAtUtc && subscription.Id.CompareTo(cursorSubscriptionId) < 0));
        }

        var query = ProjectSummaries(subscriptions);
        if (request.BillingAccountOwnerType.HasValue)
        {
            query = query.Where(item => item.BillingAccountOwnerType == request.BillingAccountOwnerType.Value);
        }

        if (request.StakeholderId.HasValue)
        {
            query = query.Where(item => item.StakeholderId == request.StakeholderId.Value);
        }

        if (request.AllocationSource.HasValue)
        {
            query = query.Where(item => item.AllocationSource == request.AllocationSource.Value);
        }

        var rows = await query
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.SubscriptionId)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);
        var items = rows.Select(ToReadModel).ToList();

        var hasMore = items.Count > request.Limit;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        return new AdminSubscriptionListPage(items, hasMore);
    }

    public async Task<AdminSubscriptionDetailReadModel?> GetSubscriptionDetailAsync(
        Guid subscriptionId,
        int historyLimit,
        CancellationToken cancellationToken)
    {
        var subscriptions = dbContext.Subscriptions.AsNoTracking().Where(subscription => subscription.Id == subscriptionId);
        var summaryRow = await ProjectSummaries(subscriptions).SingleOrDefaultAsync(cancellationToken);
        if (summaryRow is null)
        {
            return null;
        }

        var summary = ToReadModel(summaryRow);

        var lifecycle = await subscriptions
            .Select(subscription => new
            {
                subscription.CancellationRequestedAtUtc,
                subscription.CancellationReason,
                subscription.CancelledAtUtc,
                subscription.EndedAtUtc,
                subscription.LastSuccessfulPaymentTransactionId
            })
            .SingleAsync(cancellationToken);

        var stakeholder = summary.StakeholderId.HasValue
            ? await dbContext.Stakeholders.AsNoTracking()
                .Where(item => item.TenantId == summary.TenantId && item.Id == summary.StakeholderId.Value)
                .Select(item => new AdminSubscriptionStakeholderReadModel(item.Id, item.FirstName, item.LastName))
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        var plan = await ProjectPlans(dbContext.SubscriptionPlans.AsNoTracking().Where(item => item.Id == summary.PlanId))
            .SingleAsync(cancellationToken);

        var purchase = await dbContext.SubscriptionPurchases.AsNoTracking()
            .Where(item => item.SubscriptionId == subscriptionId)
            .Select(item => new AdminSubscriptionPurchaseReadModel(
                item.Id,
                item.PaymentTransactionId,
                item.ExpectedAmount,
                item.ExpectedCurrencyId,
                item.InitiatedByStakeholderId,
                item.CreatedAtUtc,
                item.FulfilledAtUtc,
                item.FulfillmentBlockedAtUtc,
                item.FulfillmentBlockedReason))
            .SingleOrDefaultAsync(cancellationToken);

        var periods = await dbContext.SubscriptionPeriods.AsNoTracking()
            .Where(item => item.SubscriptionId == subscriptionId)
            .OrderByDescending(item => item.PeriodStartUtc)
            .Take(historyLimit)
            .Select(item => new AdminSubscriptionPeriodReadModel(
                item.Id,
                item.PeriodStartUtc,
                item.PeriodEndUtc,
                item.PlanCode,
                item.Price,
                item.CurrencyId,
                item.BillingInterval,
                item.ActivationSource,
                item.PaymentTransactionId,
                item.OperationId))
            .ToListAsync(cancellationToken);

        var statusHistory = await dbContext.SubscriptionStatusHistory.AsNoTracking()
            .Where(item => item.SubscriptionId == subscriptionId)
            .OrderByDescending(item => item.OccurredAtUtc)
            .ThenByDescending(item => item.Id)
            .Take(historyLimit)
            .Select(item => new AdminSubscriptionStatusHistoryReadModel(
                item.Id,
                item.PreviousStatus,
                item.NewStatus,
                item.Reason,
                item.Details,
                item.ActorId,
                item.OperationId,
                item.PaymentTransactionId,
                item.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        var allocations = await dbContext.SubscriptionAllocations.AsNoTracking()
            .Where(item => item.SubscriptionId == subscriptionId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(historyLimit)
            .Select(item => new AdminSubscriptionAllocationReadModel(
                item.Id,
                item.AllocationSource,
                item.Reason,
                item.AllocatedByActorId,
                item.OperationId,
                item.PlanId,
                item.PlanCode,
                item.NominalPrice,
                item.CurrencyId,
                item.BillingInterval,
                item.StartAtUtc,
                item.EndAtUtc,
                item.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var renewalRows = await dbContext.SubscriptionRenewals.AsNoTracking()
            .Where(item => item.SubscriptionId == subscriptionId)
            .OrderByDescending(item => item.TargetPeriodStartUtc)
            .Take(historyLimit)
            .Select(item => new
            {
                item.Id,
                item.TargetPeriodStartUtc,
                item.TargetPeriodEndUtc,
                item.ExpectedPrice,
                item.CurrencyId,
                item.Status,
                item.CreatedAtUtc,
                item.CompletedAtUtc,
                item.NextRetryAtUtc
            })
            .ToListAsync(cancellationToken);
        var renewalIds = renewalRows.Select(item => item.Id).ToArray();
        var attempts = await dbContext.SubscriptionRenewalAttempts.AsNoTracking()
            .Where(item => renewalIds.Contains(item.RenewalId))
            .OrderBy(item => item.AttemptNumber)
            .Select(item => new
            {
                item.RenewalId,
                Attempt = new AdminSubscriptionRenewalAttemptReadModel(
                    item.Id,
                    item.AttemptNumber,
                    item.PaymentTransactionId,
                    item.Status,
                    item.FailureCode,
                    item.FailureReason,
                    item.CreatedAtUtc,
                    item.CompletedAtUtc)
            })
            .ToListAsync(cancellationToken);
        var attemptsByRenewal = attempts.ToLookup(item => item.RenewalId, item => item.Attempt);
        var renewals = renewalRows
            .Select(item => new AdminSubscriptionRenewalReadModel(
                item.Id,
                item.TargetPeriodStartUtc,
                item.TargetPeriodEndUtc,
                item.ExpectedPrice,
                item.CurrencyId,
                item.Status,
                item.CreatedAtUtc,
                item.CompletedAtUtc,
                item.NextRetryAtUtc,
                attemptsByRenewal[item.Id].ToArray()))
            .ToArray();

        var renewalPaymentIds =
            from attempt in dbContext.SubscriptionRenewalAttempts.AsNoTracking()
            join renewal in dbContext.SubscriptionRenewals.AsNoTracking() on attempt.RenewalId equals renewal.Id
            where renewal.SubscriptionId == subscriptionId && attempt.PaymentTransactionId != null
            select new { PaymentTransactionId = attempt.PaymentTransactionId!.Value, RenewalId = (Guid?)renewal.Id };
        var purchasePaymentIds = dbContext.SubscriptionPurchases.AsNoTracking()
            .Where(item => item.SubscriptionId == subscriptionId)
            .Select(item => new { item.PaymentTransactionId, RenewalId = (Guid?)null });
        var payments = await (
                from correlation in renewalPaymentIds.Concat(purchasePaymentIds)
                join payment in dbContext.PaymentTransactions.AsNoTracking() on correlation.PaymentTransactionId equals payment.Id
                join currency in dbContext.Currencies.AsNoTracking() on payment.CurrencyId equals currency.Id
                orderby payment.CreatedAtUtc descending
                select new AdminSubscriptionPaymentReadModel(
                    payment.Id,
                    payment.MerchantReference,
                    payment.Amount,
                    payment.CurrencyId,
                    currency.CurrencyCode,
                    payment.PaymentStatus,
                    correlation.RenewalId,
                    payment.CreatedAtUtc,
                    payment.CompletedAtUtc,
                    payment.FailedAtUtc))
            .Take(historyLimit)
            .ToListAsync(cancellationToken);

        return new AdminSubscriptionDetailReadModel(
            summary,
            lifecycle.CancellationRequestedAtUtc,
            lifecycle.CancellationReason,
            lifecycle.CancelledAtUtc,
            lifecycle.EndedAtUtc,
            lifecycle.LastSuccessfulPaymentTransactionId,
            stakeholder,
            plan,
            purchase,
            periods,
            payments,
            renewals,
            statusHistory,
            allocations);
    }

    public async Task<IReadOnlyList<AdminSubscriptionPlanReadModel>> ListPlansAsync(
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var plans = dbContext.SubscriptionPlans.AsNoTracking();
        if (isActive.HasValue)
        {
            plans = plans.Where(plan => plan.IsActive == isActive.Value);
        }

        return await ProjectPlans(plans.OrderBy(plan => plan.Code))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AdminSubscriptionPlanReadModel> ProjectPlans(IQueryable<SubscriptionPlan> plans) =>
        from plan in plans
        join currency in dbContext.Currencies.AsNoTracking() on plan.CurrencyId equals currency.Id
        select new AdminSubscriptionPlanReadModel(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Price,
            plan.CurrencyId,
            currency.CurrencyCode,
            plan.BillingInterval,
            plan.IsActive,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc);

    private IQueryable<SummaryRow> ProjectSummaries(
        IQueryable<Domain.Subscriptions.Entities.Subscription> subscriptions) =>
        from subscription in subscriptions
        join tenant in dbContext.Tenants.AsNoTracking() on subscription.TenantId equals tenant.Id
        join billingAccount in dbContext.BillingAccounts.AsNoTracking() on subscription.BillingAccountId equals billingAccount.Id
        join plan in dbContext.SubscriptionPlans.AsNoTracking() on subscription.PlanId equals plan.Id
        join allocation in dbContext.SubscriptionAllocations.AsNoTracking()
            on subscription.Id equals allocation.SubscriptionId into allocations
        from allocation in allocations.DefaultIfEmpty()
        select new SummaryRow
        {
            SubscriptionId = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = tenant.Name,
            BillingAccountId = subscription.BillingAccountId,
            BillingAccountOwnerType = billingAccount.OwnerType,
            StakeholderId = billingAccount.StakeholderId,
            BillingAccountDisplayName = billingAccount.DisplayName,
            PlanId = subscription.PlanId,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            Status = subscription.Status,
            CurrentPeriodStartUtc = subscription.CurrentPeriodStartUtc,
            CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc,
            EntitlementStartUtc = subscription.EntitlementStartUtc,
            EntitlementEndUtc = subscription.EntitlementEndUtc,
            NextBillingAtUtc = subscription.NextBillingAtUtc,
            AutoRenew = subscription.AutoRenew,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            CreatedAtUtc = subscription.CreatedAtUtc,
            HasPurchase = dbContext.SubscriptionPurchases.Any(purchase => purchase.SubscriptionId == subscription.Id),
            AllocationSource = allocation != null ? (SubscriptionAllocationSource?)allocation.AllocationSource : null
        };

    private static AdminSubscriptionListItemReadModel ToReadModel(SummaryRow row) =>
        new(
            row.SubscriptionId,
            row.TenantId,
            row.TenantName,
            row.BillingAccountId,
            row.BillingAccountOwnerType,
            row.StakeholderId,
            row.BillingAccountDisplayName,
            row.PlanId,
            row.PlanCode,
            row.PlanName,
            row.Status,
            row.CurrentPeriodStartUtc,
            row.CurrentPeriodEndUtc,
            row.EntitlementStartUtc,
            row.EntitlementEndUtc,
            row.NextBillingAtUtc,
            row.AutoRenew,
            row.CancelAtPeriodEnd,
            row.CreatedAtUtc,
            row.AllocationSource.HasValue
                ? SubscriptionOrigin.Allocation
                : row.HasPurchase ? SubscriptionOrigin.Purchase : SubscriptionOrigin.Unknown,
            row.AllocationSource);

    private sealed class SummaryRow
    {
        public Guid SubscriptionId { get; init; }
        public Guid TenantId { get; init; }
        public string TenantName { get; init; } = string.Empty;
        public Guid BillingAccountId { get; init; }
        public BillingAccountOwnerType BillingAccountOwnerType { get; init; }
        public Guid? StakeholderId { get; init; }
        public string BillingAccountDisplayName { get; init; } = string.Empty;
        public Guid PlanId { get; init; }
        public string PlanCode { get; init; } = string.Empty;
        public string PlanName { get; init; } = string.Empty;
        public SubscriptionStatus Status { get; init; }
        public DateTimeOffset? CurrentPeriodStartUtc { get; init; }
        public DateTimeOffset? CurrentPeriodEndUtc { get; init; }
        public DateTimeOffset? EntitlementStartUtc { get; init; }
        public DateTimeOffset? EntitlementEndUtc { get; init; }
        public DateTimeOffset? NextBillingAtUtc { get; init; }
        public bool AutoRenew { get; init; }
        public bool CancelAtPeriodEnd { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
        public bool HasPurchase { get; init; }
        public SubscriptionAllocationSource? AllocationSource { get; init; }
    }
}
