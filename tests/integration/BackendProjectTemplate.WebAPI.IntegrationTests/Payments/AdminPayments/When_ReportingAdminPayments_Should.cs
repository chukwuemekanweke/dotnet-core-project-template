using BackendProjectTemplate.Application.Payments.Features.GetAdminPaymentDetail;
using BackendProjectTemplate.Application.Payments.Features.GetAdminPayments;
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
public sealed class When_ReportingAdminPayments_Should(ContainersFixture fixture)
    : AdminSubscriptionIntegrationTestBase(fixture)
{
    private readonly string _referencePrefix = $"admin-report-{Guid.CreateVersion7():N}";
    private readonly List<Guid> _paymentProviderIds = [];

    [Fact]
    public async Task AuthorizeFilterPaginateAndReturnSafeDiagnosticDetail()
    {
        using (var anonymous = await Client.GetAsync(EndpointUrl.AdminPayments.V1))
        {
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        await AuthenticateAsync();
        using (var forbidden = await Client.GetAsync(EndpointUrl.AdminPayments.V1))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        await GrantRoleAsync(ApplicationRoles.Administrator);
        var seeded = await SeedPaymentsAsync();
        var query = $"TenantId={TargetTenantId}&PaymentProviderId={seeded.ProviderId}&CurrencyId={CurrencyId}" +
                    $"&PaymentIntent=WalletTopUp&PaymentStatus=Succeeded&PaymentMethodType=Card" +
                    $"&Search={Uri.EscapeDataString(_referencePrefix)}&Limit=1";
        using var firstResponse = await Client.GetAsync($"{EndpointUrl.AdminPayments.V1}?{query}");
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await firstResponse.Content.ReadAsStringAsync());
        var firstPage = (await firstResponse.Content.ReadFromJsonAsync<GetAdminPaymentsResult>())!;
        firstPage.Items.ShouldHaveSingleItem();
        firstPage.NextCursor.ShouldNotBeNull();
        firstPage.Items[0].StakeholderDisplayName.ShouldBe("Grace Hopper");

        using var secondResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminPayments.V1}?{query}&Cursor={Uri.EscapeDataString(firstPage.NextCursor)}");
        var secondPage = (await secondResponse.Content.ReadFromJsonAsync<GetAdminPaymentsResult>())!;
        secondPage.Items.ShouldHaveSingleItem();
        secondPage.NextCursor.ShouldBeNull();
        firstPage.Items.Concat(secondPage.Items).Select(item => item.TransactionId)
            .ShouldBe([seeded.FirstPaymentId, seeded.SecondPaymentId], ignoreOrder: true);

        using var invalidCursor = await Client.GetAsync($"{EndpointUrl.AdminPayments.V1}?Cursor=not-base64");
        invalidCursor.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var invalidEnum = await Client.GetAsync($"{EndpointUrl.AdminPayments.V1}?PaymentStatus=Successful");
        invalidEnum.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var detailResponse = await Client.GetAsync(
            $"{EndpointUrl.AdminPayments.DetailV1(seeded.FirstPaymentId)}?WebhookHistoryLimit=1");
        var rawResponse = await detailResponse.Content.ReadAsStringAsync();
        detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK, rawResponse);
        rawResponse.ShouldNotContain("TOP_SECRET_RAW_PAYLOAD");
        var detail = System.Text.Json.JsonSerializer.Deserialize<GetAdminPaymentDetailResponse>(
            rawResponse,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        detail.PaymentStatus.ShouldBe("Succeeded");
        detail.PaymentProviderId.ShouldBe(seeded.ProviderId);
        detail.Webhooks.ShouldHaveSingleItem().WebhookEventName.ShouldBe("payment.succeeded");
        detail.Fulfillment.Type.ShouldBe("WalletTopUp");
        detail.Fulfillment.Status.ShouldBe("Fulfilled");
        detail.Reconciliation.LastStatusCheckAtUtc.ShouldNotBeNull();

        using var missing = await Client.GetAsync(EndpointUrl.AdminPayments.DetailV1(Guid.CreateVersion7()));
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public override async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.PaymentWebhookInboxes.IgnoreQueryFilters()
                .Where(item => item.MerchantReference != null && item.MerchantReference.StartsWith(_referencePrefix))
                .ExecuteDeleteAsync();
            await dbContext.WalletTransactions.IgnoreQueryFilters()
                .Where(item => item.MerchantReference.StartsWith(_referencePrefix))
                .ExecuteDeleteAsync();
            await dbContext.Wallets.IgnoreQueryFilters()
                .Where(item => item.TenantId == TargetTenantId && item.StakeholderId == TargetStakeholderId && item.CurrencyId == CurrencyId)
                .ExecuteDeleteAsync();
            await dbContext.PaymentProviders.IgnoreQueryFilters()
                .Where(item => _paymentProviderIds.Contains(item.Id))
                .ExecuteDeleteAsync();
        }

        await base.DisposeAsync();
    }

    private async Task<(Guid FirstPaymentId, Guid SecondPaymentId, Guid ProviderId)> SeedPaymentsAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var providers = new[]
        {
            PaymentProvider.Create("Admin Report Primary", $"{_referencePrefix}-primary", true),
            PaymentProvider.Create("Admin Report Isolated", $"{_referencePrefix}-isolated", true)
        };
        _paymentProviderIds.AddRange(providers.Select(item => item.Id));
        dbContext.PaymentProviders.AddRange(providers);
        var stakeholder = await dbContext.Stakeholders.SingleAsync(item => item.Id == TargetStakeholderId);
        var now = UtcNow();
        var first = CreateSucceededPayment(providers[0].Id, stakeholder.AppUserId, stakeholder.CountryId, "one", now);
        var second = CreateSucceededPayment(providers[0].Id, stakeholder.AppUserId, stakeholder.CountryId, "two", now.AddSeconds(1));
        dbContext.PaymentTransactions.AddRange(first, second);

        var wallet = Wallet.Create(TargetStakeholderId, TargetTenantId, CurrencyId);
        wallet.Credit(first.Amount);
        dbContext.Wallets.Add(wallet);
        dbContext.WalletTransactions.Add(WalletTransaction.CreateCredit(
            wallet.Id,
            first.Id,
            first.MerchantReference,
            first.Amount,
            CurrencyId,
            WalletTransactionCategory.WalletFunding,
            "Wallet top-up",
            null));

        var includedWebhook = PaymentWebhookInbox.Create(
            providers[0].Id,
            first.MerchantReference,
            first.ProviderReference,
            "payment.succeeded",
            $"evt-{Guid.CreateVersion7():N}",
            "{\"secret\":\"TOP_SECRET_RAW_PAYLOAD\"}",
            SignatureValidationStatus.Valid,
            "signature_verified",
            now);
        includedWebhook.MarkProcessed("signature_verified", now.AddSeconds(1));
        var olderIncludedWebhook = PaymentWebhookInbox.Create(
            providers[0].Id,
            first.MerchantReference,
            first.ProviderReference,
            "payment.processing",
            $"evt-{Guid.CreateVersion7():N}",
            "{}",
            SignatureValidationStatus.Valid,
            "signature_verified",
            now.AddSeconds(-1));
        var isolatedWebhook = PaymentWebhookInbox.Create(
            providers[1].Id,
            first.MerchantReference,
            first.ProviderReference,
            "wrong-provider-event",
            $"evt-{Guid.CreateVersion7():N}",
            "{}",
            SignatureValidationStatus.Valid,
            "signature_verified",
            now.AddSeconds(2));
        dbContext.PaymentWebhookInboxes.AddRange(includedWebhook, olderIncludedWebhook, isolatedWebhook);
        await dbContext.SaveChangesAsync();
        return (first.Id, second.Id, providers[0].Id);
    }

    private PaymentTransaction CreateSucceededPayment(
        Guid providerId,
        Guid initiatedByUserId,
        Guid countryId,
        string suffix,
        DateTimeOffset now)
    {
        var payment = PaymentTransaction.Create(
            $"{_referencePrefix}-{suffix}",
            PaymentIntent.WalletTopUp,
            providerId,
            100000m,
            CurrencyId,
            countryId,
            initiatedByUserId,
            TargetStakeholderId,
            TargetTenantId);
        payment.SetPaymentMethodType(PaymentMethodType.Card);
        payment.MarkInitiated($"provider-{_referencePrefix}-{suffix}", null, now.AddHours(1), "payment_initiated");
        payment.MarkSucceeded(payment.ProviderReference, "reconciliation_confirmed_success", now);
        payment.RecordStatusCheck(now);
        return payment;
    }
}
