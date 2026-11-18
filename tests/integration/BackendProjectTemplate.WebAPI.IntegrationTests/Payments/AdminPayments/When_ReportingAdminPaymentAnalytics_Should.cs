using BackendProjectTemplate.Application.Payments.Analytics;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Payments.AdminPayments;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReportingAdminPaymentAnalytics_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    private Guid _providerId;
    private Guid _secondCurrencyId;

    [Fact]
    public async Task AuthorizeAggregateFilterAndKeepCurrenciesSeparate()
    {
        using (var anonymous = await Client.GetAsync(EndpointUrl.AdminPaymentAnalytics.SummaryV1))
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        await AuthenticateAsync();
        using (var forbidden = await Client.GetAsync(EndpointUrl.AdminPaymentAnalytics.SummaryV1))
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await GrantRoleAsync(ApplicationRoles.FinanceOfficer);
        await SeedAnalyticsPaymentsAsync();
        var today = DateOnly.FromDateTime(UtcNow().UtcDateTime);
        var query = $"from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}&tenantId={TargetTenantId}";

        using var summaryResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.SummaryV1}?{query}");
        summaryResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await summaryResponse.Content.ReadAsStringAsync());
        var summary = (await summaryResponse.Content.ReadFromJsonAsync<PaymentAnalyticsSummaryResponse>())!;
        summary.TransactionCount.ShouldBe(4);
        summary.SuccessfulCount.ShouldBe(1);
        summary.UnsuccessfulCount.ShouldBe(2);
        summary.ProcessingCount.ShouldBe(1);
        summary.SuccessRate.ShouldBe(33.33m);
        summary.Currencies.Count.ShouldBe(2);
        summary.Currencies.Sum(item => item.TransactionCount).ShouldBe(4);

        foreach (var interval in new[] { "Day", "Week", "Month", "Year" })
        {
            using var timelineResponse = await Client.GetAsync(
                $"{EndpointUrl.AdminPaymentAnalytics.TimelineV1}?{query}&interval={interval}");
            timelineResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await timelineResponse.Content.ReadAsStringAsync());
            var timeline = (await timelineResponse.Content.ReadFromJsonAsync<PaymentAnalyticsTimelineResponse>())!;
            timeline.Buckets.Sum(item => item.TransactionCount).ShouldBe(4);
            timeline.Buckets.Select(item => item.CurrencyCode).Distinct().Count().ShouldBe(2);
        }

        using var statusesResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.StatusesV1}?{query}");
        var statuses = (await statusesResponse.Content.ReadFromJsonAsync<PaymentAnalyticsStatusesResponse>())!;
        statuses.Statuses.Sum(item => item.TransactionCount).ShouldBe(summary.TransactionCount);

        using var providersResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.ProvidersV1}?{query}");
        var providers = (await providersResponse.Content.ReadFromJsonAsync<PaymentAnalyticsProvidersResponse>())!;
        providers.Providers.Sum(item => item.TransactionCount).ShouldBe(summary.TransactionCount);

        using var intentsResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.IntentsV1}?{query}");
        var intents = (await intentsResponse.Content.ReadFromJsonAsync<PaymentAnalyticsIntentsResponse>())!;
        intents.Intents.Sum(item => item.TransactionCount).ShouldBe(summary.TransactionCount);

        using var filteredResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminPaymentAnalytics.SummaryV1}?{query}&currencyId={CurrencyId}&paymentStatus=Succeeded&paymentProviderId={_providerId}&paymentIntent=WalletTopUp");
        var filtered = (await filteredResponse.Content.ReadFromJsonAsync<PaymentAnalyticsSummaryResponse>())!;
        filtered.TransactionCount.ShouldBe(1);
        filtered.Currencies.ShouldHaveSingleItem().SuccessfulVolume.ShouldBe(100m);

        using var invalidRange = await Client.GetAsync(
            $"{EndpointUrl.AdminPaymentAnalytics.SummaryV1}?from=2026-01-01&to=2027-12-31");
        invalidRange.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        await RevokeRoleAsync(ApplicationRoles.FinanceOfficer);
        await GrantRoleAsync(ApplicationRoles.Administrator);
        using var administratorResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.SummaryV1}?{query}");
        administratorResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    public override async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PaymentTransactions.Where(item => item.TenantId == TargetTenantId).ExecuteDeleteAsync();
            if (_providerId != Guid.Empty)
                await dbContext.PaymentProviders.IgnoreQueryFilters().Where(item => item.Id == _providerId).ExecuteDeleteAsync();
            if (_secondCurrencyId != Guid.Empty)
                await dbContext.Currencies.IgnoreQueryFilters().Where(item => item.Id == _secondCurrencyId).ExecuteDeleteAsync();
        }
        await base.DisposeAsync();
    }

    private async Task SeedAnalyticsPaymentsAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stakeholder = await dbContext.Stakeholders.SingleAsync(item => item.Id == TargetStakeholderId);
        var provider = PaymentProvider.Create("Analytics Provider", $"analytics-{Guid.CreateVersion7():N}", true);
        var currencyToken = Guid.CreateVersion7().ToString("N");
        var secondCurrency = Currency.Create(currencyToken[^3..].ToUpperInvariant(), "Analytics USD", true);
        _providerId = provider.Id;
        _secondCurrencyId = secondCurrency.Id;
        dbContext.PaymentProviders.Add(provider);
        dbContext.Currencies.Add(secondCurrency);

        var now = UtcNow();
        var succeeded = CreatePayment(provider.Id, CurrencyId, stakeholder.AppUserId, stakeholder.CountryId, 100m, "succeeded");
        succeeded.MarkInitiated("analytics-succeeded", null, now.AddHours(1), null);
        succeeded.MarkSucceeded(succeeded.ProviderReference, null, now);
        var failed = CreatePayment(provider.Id, CurrencyId, stakeholder.AppUserId, stakeholder.CountryId, 50m, "failed");
        failed.MarkInitiated("analytics-failed", null, now.AddHours(1), null);
        failed.MarkFailed(failed.ProviderReference, "declined", null, now);
        var pending = CreatePayment(provider.Id, secondCurrency.Id, stakeholder.AppUserId, stakeholder.CountryId, 25m, "pending");
        var expired = CreatePayment(provider.Id, secondCurrency.Id, stakeholder.AppUserId, stakeholder.CountryId, 20m, "expired");
        expired.MarkInitiated("analytics-expired", null, now.AddHours(1), null);
        expired.MarkExpired(expired.ProviderReference, "expired", null);
        dbContext.PaymentTransactions.AddRange(succeeded, failed, pending, expired);
        await dbContext.SaveChangesAsync();
    }

    private PaymentTransaction CreatePayment(
        Guid providerId,
        Guid currencyId,
        Guid userId,
        Guid countryId,
        decimal amount,
        string suffix) => PaymentTransaction.Create(
        $"analytics-{Guid.CreateVersion7():N}-{suffix}", PaymentIntent.WalletTopUp, providerId, amount,
        currencyId, countryId, userId, TargetStakeholderId, TargetTenantId);
}
