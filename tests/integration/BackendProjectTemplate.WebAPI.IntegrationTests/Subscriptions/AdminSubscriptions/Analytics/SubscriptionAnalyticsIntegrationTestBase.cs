using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions.Analytics;

/// <summary>
/// Authenticates an administrator and seeds subscription histories through the real domain lifecycle at
/// historical instants. Only <see cref="SubscriptionPeriod.CreatedAtUtc"/> is backdated afterwards, because the
/// audit interceptor stamps it with the current time while analytics treats it as the moment the period was
/// persisted.
/// </summary>
public abstract class SubscriptionAnalyticsIntegrationTestBase(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    private Guid _providerId;

    protected DateOnly Today { get; private set; }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Today = DateOnly.FromDateTime(UtcNow().UtcDateTime);
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var provider = PaymentProvider.Create(
                "Subscription Analytics Provider",
                $"subscription-analytics-{Guid.CreateVersion7():N}",
                true);
            dbContext.PaymentProviders.Add(provider);
            await dbContext.SaveChangesAsync();
            _providerId = provider.Id;
            TrackPaymentProviderForCleanup(provider.Id);
        }

        await AuthenticateAsync();
        await GrantRoleAsync(ApplicationRoles.Administrator);
    }

    /// <summary>Noon UTC <paramref name="days"/> calendar days before today.</summary>
    protected DateTimeOffset DaysAgo(int days) =>
        new(Today.AddDays(-days).ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

    protected async Task<SubscriptionPlan> CreatePlanAsync(
        string suffix,
        decimal price,
        BillingInterval interval,
        Guid currencyId)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = SubscriptionPlan.Create($"{PlanCodePrefix}_{suffix}", $"Plan {suffix}", price, currencyId, interval, true);
        dbContext.SubscriptionPlans.Add(plan);
        await dbContext.SaveChangesAsync();
        return plan;
    }

    protected async Task RepricePlanAsync(Guid planId, decimal price)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.SubscriptionPlanPrices.IgnoreQueryFilters()
            .Where(item => item.PlanId == planId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Price, price));
    }

    /// <summary>
    /// Seeds a paid subscription for a stakeholder-owned BillingAccount (a new stakeholder unless one is given)
    /// and replays the requested lifecycle. Returns the subscription id.
    /// </summary>
    protected async Task<Guid> SeedPaidSubscriptionAsync(
        SubscriptionPlan plan,
        DateTimeOffset activatedAtUtc,
        bool autoRenew,
        PaidLifecycle? lifecycle = null)
    {
        lifecycle ??= new PaidLifecycle();
        var stakeholderId = lifecycle.StakeholderId ?? await AddTargetStakeholderAsync();
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stakeholder = await dbContext.Stakeholders.SingleAsync(item => item.Id == stakeholderId);
        var account = await dbContext.BillingAccounts.SingleOrDefaultAsync(item =>
            item.TenantId == TargetTenantId &&
            item.OwnerType == BillingAccountOwnerType.Stakeholder &&
            item.StakeholderId == stakeholderId);
        if (account is null)
        {
            account = BillingAccount.Create(TargetTenantId, BillingAccountOwnerType.Stakeholder, stakeholderId, "Analytics account");
            dbContext.BillingAccounts.Add(account);
        }

        var planPrice = plan.Prices.Single();
        PaymentTransaction NewPayment()
        {
            var payment = PaymentTransaction.Create(
                $"analytics-{Guid.CreateVersion7():N}",
                PaymentIntent.Subscription,
                _providerId,
                planPrice.Price,
                plan.CurrencyId,
                stakeholder.CountryId,
                stakeholder.AppUserId,
                stakeholder.Id,
                TargetTenantId);
            dbContext.PaymentTransactions.Add(payment);
            return payment;
        }

        var subscription = Subscription.CreatePending(
            TargetTenantId,
            account.Id,
            plan.Id,
            planPrice.BillingInterval,
            autoRenew,
            activatedAtUtc.AddMinutes(-1),
            Guid.CreateVersion7(),
            stakeholder.Id.ToString());
        var initialPayment = NewPayment();
        var purchase = SubscriptionPurchase.Create(
            TargetTenantId,
            account.Id,
            subscription.Id,
            plan,
            planPrice.BillingInterval,
            initialPayment.Id,
            stakeholder.Id);
        var firstPeriod = subscription.ActivateFromPayment(purchase, initialPayment.Id, activatedAtUtc, Guid.CreateVersion7());
        purchase.MarkFulfilled(activatedAtUtc);
        var persistedAt = new Dictionary<Guid, DateTimeOffset> { [firstPeriod.Id] = activatedAtUtc };
        dbContext.Subscriptions.Add(subscription);
        dbContext.SubscriptionPurchases.Add(purchase);

        if (lifecycle.Renewal != RenewalOutcome.None)
        {
            var boundary = firstPeriod.PeriodEndUtc;
            var authorization = RecurringPaymentAuthorization.Create(
                TargetTenantId,
                account.Id,
                _providerId,
                $"customer-{Guid.CreateVersion7():N}",
                plan.CurrencyId,
                initialPayment.Id,
                activatedAtUtc,
                activatedAtUtc);
            var renewal = SubscriptionRenewal.Create(subscription, firstPeriod, boundary.AddDays(-3));
            var declinedAttempt = renewal.StartAttempt(authorization.Id, NewPayment().Id, $"renewal-{renewal.Id}-1", boundary);
            declinedAttempt.MarkFailed("declined", "Card declined", boundary, boundary.AddDays(1));
            renewal.MarkRetryScheduled(boundary.AddDays(1));
            var retryPayment = NewPayment();
            var retryAttempt = renewal.StartAttempt(authorization.Id, retryPayment.Id, $"renewal-{renewal.Id}-2", boundary.AddDays(1));
            if (lifecycle.Renewal == RenewalOutcome.RecoveredAfterFailure)
            {
                retryAttempt.MarkSucceeded(boundary.AddDays(1));
                var renewedPeriod = subscription.RenewFromPayment(renewal, retryPayment.Id, boundary.AddDays(1), Guid.CreateVersion7());
                // A duplicate delivery of the same payment must resolve to the same period.
                subscription.RenewFromPayment(renewal, retryPayment.Id, boundary.AddDays(1), Guid.CreateVersion7())
                    .ShouldBeSameAs(renewedPeriod);
                renewal.MarkFulfilled(boundary.AddDays(1));
                persistedAt[renewedPeriod.Id] = boundary.AddDays(1);
            }
            else
            {
                retryAttempt.MarkFailed("declined", "Card declined", boundary.AddDays(2), null);
                renewal.MarkExhausted(boundary.AddDays(2));
            }

            dbContext.RecurringPaymentAuthorizations.Add(authorization);
            dbContext.SubscriptionRenewals.Add(renewal);
        }

        if (lifecycle.ScheduleCancellationAtUtc is { } scheduledAt)
            subscription.ScheduleCancellation(scheduledAt, Guid.CreateVersion7(), "analytics-admin", "Customer request");
        if (lifecycle.CancelImmediatelyAtUtc is { } cancelledAt)
            subscription.CancelImmediately(cancelledAt, Guid.CreateVersion7(), "analytics-admin", "Customer request");
        if (lifecycle.EndAtUtc is { } endedAt)
            subscription.End(endedAt, Guid.CreateVersion7());

        await dbContext.SaveChangesAsync();
        foreach (var (periodId, createdAtUtc) in persistedAt)
        {
            await dbContext.SubscriptionPeriods
                .Where(item => item.Id == periodId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedAtUtc, createdAtUtc));
        }

        return subscription.Id;
    }

    /// <summary>Seeds a purchase whose initial payment never succeeded, so it expired without entitlement.</summary>
    protected async Task SeedUnpaidExpiredPurchaseAsync(SubscriptionPlan plan, DateTimeOffset expiredAtUtc)
    {
        var stakeholderId = await AddTargetStakeholderAsync();
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stakeholder = await dbContext.Stakeholders.SingleAsync(item => item.Id == stakeholderId);
        var account = BillingAccount.Create(TargetTenantId, BillingAccountOwnerType.Stakeholder, stakeholderId, "Unpaid account");
        var payment = PaymentTransaction.Create(
            $"analytics-unpaid-{Guid.CreateVersion7():N}",
            PaymentIntent.Subscription,
            _providerId,
            plan.Prices.Single().Price,
            plan.CurrencyId,
            stakeholder.CountryId,
            stakeholder.AppUserId,
            stakeholder.Id,
            TargetTenantId);
        var subscription = Subscription.CreatePending(
            TargetTenantId,
            account.Id,
            plan.Id,
            plan.Prices.Single().BillingInterval,
            autoRenew: true,
            expiredAtUtc.AddHours(-1),
            Guid.CreateVersion7(),
            stakeholderId.ToString());
        subscription.ExpireUnpaidPurchase(payment.Id, expiredAtUtc, Guid.CreateVersion7()).ShouldBeTrue();
        dbContext.BillingAccounts.Add(account);
        dbContext.PaymentTransactions.Add(payment);
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();
    }

    protected async Task<T> GetAsync<T>(string url)
    {
        using var response = await Client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    protected enum RenewalOutcome
    {
        None,
        RecoveredAfterFailure,
        Exhausted
    }

    protected sealed record PaidLifecycle(
        RenewalOutcome Renewal = RenewalOutcome.None,
        DateTimeOffset? ScheduleCancellationAtUtc = null,
        DateTimeOffset? CancelImmediatelyAtUtc = null,
        DateTimeOffset? EndAtUtc = null,
        Guid? StakeholderId = null);
}
