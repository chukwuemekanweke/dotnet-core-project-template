using BackendProjectTemplate.Application.Payments.Analytics;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Analytics;
using BackendProjectTemplate.Domain.Common.Authorization;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using BackendProjectTemplate.Infrastructure.Persistence;
using BackendProjectTemplate.WebAPI.IntegrationTests.Subscriptions.AdminSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Data.Common;
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
        var from = new DateOnly(2025, 12, 28);
        var to = new DateOnly(2026, 4, 30);
        var query = $"from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&tenantId={TargetTenantId}";

        using var summaryResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.SummaryV1}?{query}");
        summaryResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await summaryResponse.Content.ReadAsStringAsync());
        var summary = (await summaryResponse.Content.ReadFromJsonAsync<PaymentAnalyticsSummaryResponse>())!;
        summary.TransactionCount.ShouldBe(13);
        summary.SuccessfulCount.ShouldBe(4);
        summary.UnsuccessfulCount.ShouldBe(5);
        summary.UnderpaidCount.ShouldBe(1);
        summary.ProcessingCount.ShouldBe(4);
        summary.SuccessRate.ShouldBe(44.44m);
        summary.Currencies.Count.ShouldBe(2);
        summary.Currencies.Sum(item => item.TransactionCount).ShouldBe(13);
        var ngn = summary.Currencies.Single(item => item.CurrencyId == CurrencyId);
        ngn.TransactionCount.ShouldBe(10);
        ngn.UnderpaidCount.ShouldBe(1);
        ngn.AttemptedVolume.ShouldBe(786m);
        ngn.SuccessfulVolume.ShouldBe(600m);
        ngn.UnderpaidVolume.ShouldBe(10m);
        var usd = summary.Currencies.Single(item => item.CurrencyId == _secondCurrencyId);
        usd.TransactionCount.ShouldBe(3);
        usd.AttemptedVolume.ShouldBe(21m);
        usd.SuccessfulVolume.ShouldBe(5m);

        var day = await GetTimelineAsync(query, "Day");
        AssertBucket(day, new DateOnly(2025, 12, 28), CurrencyId, 2, 150m, 100m);
        AssertBucket(day, new DateOnly(2025, 12, 29), CurrencyId, 1, 30m, 0m);
        AssertBucket(day, new DateOnly(2025, 12, 29), _secondCurrencyId, 1, 5m, 5m);
        AssertBucket(day, new DateOnly(2025, 12, 30), CurrencyId, 0, 0m, 0m);
        AssertBucket(day, new DateOnly(2025, 12, 30), _secondCurrencyId, 0, 0m, 0m);
        AssertBucket(day, new DateOnly(2026, 1, 6), CurrencyId, 1, 25m, 0m, underpaidCount: 1, underpaidVolume: 10m);
        AssertBucket(day, new DateOnly(2026, 4, 30), CurrencyId, 1, 11m, 0m);
        day.Buckets.ShouldAllBe(item => item.PeriodStart == item.PeriodEnd);

        var week = await GetTimelineAsync(query, "Week");
        AssertBucket(week, new DateOnly(2025, 12, 22), CurrencyId, 2, 150m, 100m, new DateOnly(2025, 12, 28));
        AssertBucket(week, new DateOnly(2025, 12, 29), CurrencyId, 4, 100m, 0m, new DateOnly(2026, 1, 4));
        AssertBucket(week, new DateOnly(2025, 12, 29), _secondCurrencyId, 1, 5m, 5m, new DateOnly(2026, 1, 4));
        AssertBucket(week, new DateOnly(2026, 1, 5), CurrencyId, 2, 225m, 200m, new DateOnly(2026, 1, 11), 1, 10m);
        AssertBucket(week, new DateOnly(2026, 1, 5), _secondCurrencyId, 1, 7m, 0m, new DateOnly(2026, 1, 11));
        AssertBucket(week, new DateOnly(2026, 1, 12), CurrencyId, 0, 0m, 0m, new DateOnly(2026, 1, 18));
        AssertBucket(week, new DateOnly(2026, 1, 26), CurrencyId, 1, 300m, 300m, new DateOnly(2026, 2, 1));
        AssertBucket(week, new DateOnly(2026, 1, 26), _secondCurrencyId, 1, 9m, 0m, new DateOnly(2026, 2, 1));

        var month = await GetTimelineAsync(query, "Month");
        AssertBucket(month, new DateOnly(2025, 12, 1), CurrencyId, 4, 200m, 100m, new DateOnly(2025, 12, 31));
        AssertBucket(month, new DateOnly(2026, 1, 1), CurrencyId, 4, 275m, 200m, new DateOnly(2026, 1, 31), 1, 10m);
        AssertBucket(month, new DateOnly(2026, 2, 1), CurrencyId, 1, 300m, 300m, new DateOnly(2026, 2, 28));
        AssertBucket(month, new DateOnly(2026, 3, 1), CurrencyId, 0, 0m, 0m, new DateOnly(2026, 3, 31));
        AssertBucket(month, new DateOnly(2026, 3, 1), _secondCurrencyId, 0, 0m, 0m, new DateOnly(2026, 3, 31));
        AssertBucket(month, new DateOnly(2026, 4, 1), CurrencyId, 1, 11m, 0m, new DateOnly(2026, 4, 30));

        var year = await GetTimelineAsync(query, "Year");
        AssertBucket(year, new DateOnly(2025, 1, 1), CurrencyId, 4, 200m, 100m, new DateOnly(2025, 12, 31));
        AssertBucket(year, new DateOnly(2025, 1, 1), _secondCurrencyId, 1, 5m, 5m, new DateOnly(2025, 12, 31));
        AssertBucket(year, new DateOnly(2026, 1, 1), CurrencyId, 6, 586m, 500m, new DateOnly(2026, 4, 30), 1, 10m);
        AssertBucket(year, new DateOnly(2026, 1, 1), _secondCurrencyId, 2, 16m, 0m, new DateOnly(2026, 4, 30));

        using var statusesResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.StatusesV1}?{query}");
        var statuses = (await statusesResponse.Content.ReadFromJsonAsync<PaymentAnalyticsStatusesResponse>())!;
        statuses.Statuses.Sum(item => item.TransactionCount).ShouldBe(summary.TransactionCount);
        AssertStatus(statuses, PaymentStatus.Succeeded, 4, 605m);
        AssertStatus(statuses, PaymentStatus.Failed, 2, 57m);
        AssertStatus(statuses, PaymentStatus.Cancelled, 1, 30m);
        AssertStatus(statuses, PaymentStatus.Expired, 1, 20m);
        AssertStatus(statuses, PaymentStatus.Underpaid, 1, 25m);
        AssertStatus(statuses, PaymentStatus.PendingInitiation, 2, 21m);
        AssertStatus(statuses, PaymentStatus.Initiated, 2, 49m);

        using var providersResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.ProvidersV1}?{query}");
        var providers = (await providersResponse.Content.ReadFromJsonAsync<PaymentAnalyticsProvidersResponse>())!;
        providers.Providers.Sum(item => item.TransactionCount).ShouldBe(summary.TransactionCount);
        providers.Providers.Sum(item => item.UnderpaidCount).ShouldBe(1);
        providers.Providers.Sum(item => item.UnderpaidVolume).ShouldBe(10m);

        using var intentsResponse = await Client.GetAsync($"{EndpointUrl.AdminPaymentAnalytics.IntentsV1}?{query}");
        var intents = (await intentsResponse.Content.ReadFromJsonAsync<PaymentAnalyticsIntentsResponse>())!;
        intents.Intents.Sum(item => item.TransactionCount).ShouldBe(summary.TransactionCount);
        intents.Intents.Sum(item => item.UnderpaidCount).ShouldBe(1);
        intents.Intents.Sum(item => item.UnderpaidVolume).ShouldBe(10m);

        await VerifyGeneratedSqlAsync(from, to);

        using var filteredResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminPaymentAnalytics.SummaryV1}?{query}&currencyId={CurrencyId}&paymentStatus=Succeeded&paymentProviderId={_providerId}&paymentIntent=WalletTopUp");
        var filtered = (await filteredResponse.Content.ReadFromJsonAsync<PaymentAnalyticsSummaryResponse>())!;
        filtered.TransactionCount.ShouldBe(3);
        filtered.Currencies.ShouldHaveSingleItem().SuccessfulVolume.ShouldBe(600m);

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

        var definitions = new[]
        {
            new PaymentDefinition(CurrencyId, 100m, PaymentStatus.Succeeded, Utc(2025, 12, 28, 0, 0, 0), "dec-sunday-start"),
            new PaymentDefinition(CurrencyId, 50m, PaymentStatus.Failed, Utc(2025, 12, 28, 23, 59, 59), "dec-sunday-end"),
            new PaymentDefinition(CurrencyId, 30m, PaymentStatus.Cancelled, Utc(2025, 12, 29, 0, 0, 0), "dec-monday"),
            new PaymentDefinition(_secondCurrencyId, 5m, PaymentStatus.Succeeded, Utc(2025, 12, 29, 10, 0, 0), "usd-dec"),
            new PaymentDefinition(CurrencyId, 20m, PaymentStatus.Expired, Utc(2025, 12, 31, 12, 0, 0), "dec-expired"),
            new PaymentDefinition(CurrencyId, 40m, PaymentStatus.Initiated, Utc(2026, 1, 1, 0, 0, 0), "jan-initiated"),
            new PaymentDefinition(CurrencyId, 10m, PaymentStatus.PendingInitiation, Utc(2026, 1, 3, 10, 0, 0), "jan-pending"),
            new PaymentDefinition(CurrencyId, 200m, PaymentStatus.Succeeded, Utc(2026, 1, 5, 8, 0, 0), "jan-monday"),
            new PaymentDefinition(_secondCurrencyId, 7m, PaymentStatus.Failed, Utc(2026, 1, 5, 9, 0, 0), "usd-jan"),
            new PaymentDefinition(CurrencyId, 25m, PaymentStatus.Underpaid, Utc(2026, 1, 6, 9, 0, 0), "jan-underpaid", 10m),
            new PaymentDefinition(CurrencyId, 300m, PaymentStatus.Succeeded, Utc(2026, 2, 1, 11, 0, 0), "feb-success"),
            new PaymentDefinition(_secondCurrencyId, 9m, PaymentStatus.Initiated, Utc(2026, 2, 1, 12, 0, 0), "usd-feb"),
            new PaymentDefinition(CurrencyId, 11m, PaymentStatus.PendingInitiation, Utc(2026, 4, 30, 23, 59, 59), "range-end"),
            new PaymentDefinition(CurrencyId, 999m, PaymentStatus.Succeeded, Utc(2025, 12, 27, 23, 59, 59), "before-range"),
            new PaymentDefinition(CurrencyId, 999m, PaymentStatus.Succeeded, Utc(2026, 5, 1, 0, 0, 0), "after-range")
        };
        var payments = definitions.Select(definition => new
        {
            Definition = definition,
            Payment = CreatePayment(
                provider.Id,
                definition.CurrencyId,
                stakeholder.AppUserId,
                stakeholder.CountryId,
                definition.Amount,
                definition.Suffix)
        }).ToArray();
        dbContext.PaymentTransactions.AddRange(payments.Select(item => item.Payment));
        await dbContext.SaveChangesAsync();

        foreach (var item in payments)
        {
            await dbContext.PaymentTransactions
                .Where(payment => payment.Id == item.Payment.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(payment => payment.CreatedAtUtc, item.Definition.CreatedAtUtc)
                    .SetProperty(payment => payment.PaymentStatus, item.Definition.PaymentStatus)
                    .SetProperty(payment => payment.AmountPaid, item.Definition.AmountPaid));
        }
    }

    private async Task<PaymentAnalyticsTimelineResponse> GetTimelineAsync(string query, string interval)
    {
        using var response = await Client.GetAsync(
            $"{EndpointUrl.AdminPaymentAnalytics.TimelineV1}?{query}&interval={interval}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PaymentAnalyticsTimelineResponse>())!;
    }

    private static void AssertBucket(
        PaymentAnalyticsTimelineResponse timeline,
        DateOnly periodStart,
        Guid currencyId,
        long transactionCount,
        decimal attemptedVolume,
        decimal successfulVolume,
        DateOnly? periodEnd = null,
        long underpaidCount = 0,
        decimal underpaidVolume = 0m)
    {
        var bucket = timeline.Buckets.Single(item => item.PeriodStart == periodStart && item.CurrencyId == currencyId);
        bucket.PeriodEnd.ShouldBe(periodEnd ?? periodStart);
        bucket.TransactionCount.ShouldBe(transactionCount);
        bucket.AttemptedVolume.ShouldBe(attemptedVolume);
        bucket.SuccessfulVolume.ShouldBe(successfulVolume);
        bucket.UnderpaidCount.ShouldBe(underpaidCount);
        bucket.UnderpaidVolume.ShouldBe(underpaidVolume);
    }

    private static void AssertStatus(
        PaymentAnalyticsStatusesResponse response,
        PaymentStatus status,
        long transactionCount,
        decimal amount)
    {
        var rows = response.Statuses.Where(item => item.PaymentStatus == status.ToString()).ToArray();
        rows.Sum(item => item.TransactionCount).ShouldBe(transactionCount);
        rows.Sum(item => item.TotalAmount).ShouldBe(amount);
    }

    private async Task VerifyGeneratedSqlAsync(DateOnly from, DateOnly to)
    {
        using var scope = CreateScope();
        var configuredContext = scope.ServiceProvider.GetRequiredService<AppReadDbContext>();
        var interceptor = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<AppReadDbContext>()
            .UseNpgsql(configuredContext.Database.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;
        await using var readContext = new AppReadDbContext(options);
        var repository = new PaymentAnalyticsReadModelRepository(readContext);
        var filter = new PaymentAnalyticsFilter(
            new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
            new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
            null,
            null,
            null,
            null,
            TargetTenantId);

        await repository.GetSummaryAsync(filter, CancellationToken.None);
        foreach (var interval in Enum.GetValues<Interval>())
            await repository.GetTimelineAsync(filter, interval, CancellationToken.None);
        await repository.GetStatusesAsync(filter, CancellationToken.None);
        await repository.GetProvidersAsync(filter, CancellationToken.None);
        await repository.GetIntentsAsync(filter, CancellationToken.None);

        interceptor.Commands.Count.ShouldBe(9);
        interceptor.Commands.ShouldAllBe(command => command.Contains("WHERE", StringComparison.OrdinalIgnoreCase));
        interceptor.Commands.ShouldAllBe(command => command.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
        interceptor.Commands.ShouldAllBe(command => command.Contains("COUNT", StringComparison.OrdinalIgnoreCase));
        interceptor.Commands.Count(command => command.Contains("SUM", StringComparison.OrdinalIgnoreCase)).ShouldBe(8);
        interceptor.Commands.ShouldAllBe(command => command.Contains(">= @filter_FromUtc", StringComparison.Ordinal));
        interceptor.Commands.ShouldAllBe(command => command.Contains("< @filter_ToExclusiveUtc", StringComparison.Ordinal));
        interceptor.Commands[2].ShouldContain("date_trunc('day'", Case.Insensitive);
        interceptor.Commands[3].ShouldContain("date_part('dow'", Case.Insensitive);
        interceptor.Commands[4].ShouldContain("date_part('month'", Case.Insensitive);
        interceptor.Commands[5].ShouldContain("date_part('year'", Case.Insensitive);
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

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute, int second) =>
        new(year, month, day, hour, minute, second, TimeSpan.Zero);

    private sealed record PaymentDefinition(
        Guid CurrencyId,
        decimal Amount,
        PaymentStatus PaymentStatus,
        DateTimeOffset CreatedAtUtc,
        string Suffix,
        decimal? AmountPaid = null);

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
