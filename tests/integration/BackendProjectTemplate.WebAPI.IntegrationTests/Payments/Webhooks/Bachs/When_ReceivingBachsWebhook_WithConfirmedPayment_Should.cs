using BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Payments.Webhooks.Bachs;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReceivingBachsWebhook_WithConfirmedPayment_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(
        fixture,
        new Dictionary<string, string?> { ["Payments:Bachs:WebhookSecret"] = WebhookSecret }),
        IAsyncLifetime
{
    private const string WebhookSecret = "whsec_integration_test";
    private const string MerchantReference = "pay_bachs_integration";
    private const string CheckoutId = "chk_bachs_integration";

    private Guid _paymentProviderId;
    private Guid _paymentTransactionId;
    private Guid _currencyId;
    private Guid _webhookInboxId;
    private Guid _outboxMessageId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        await SeedPaymentSetupAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task PersistConfirmAndPublishOutboxEvent()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_bachs_integration",
            type = BachsWebhookEvents.CollectionSucceeded,
            created_at = DateTimeOffset.UtcNow,
            organization_id = "acct_integration",
            data = new
            {
                charge_id = "ch_integration",
                checkout_id = CheckoutId,
                reference = MerchantReference,
                status = "SUCCEEDED",
                amount = "25.00",
                currency = "NGN"
            }
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl.PaymentWebhooks.Bachs.V1)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Bachs-Signature-V2", $"t={timestamp},v1={ComputeSignature(timestamp, payload)}");

        _response = await Client.SendAsync(request);

        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendProjectTemplate.Infrastructure.Persistence.AppDbContext>();
        var transaction = await dbContext.PaymentTransactions.SingleAsync(item => item.Id == _paymentTransactionId);
        transaction.PaymentStatus.ShouldBe(PaymentStatus.Succeeded);
        var inbox = await dbContext.PaymentWebhookInboxes.SingleAsync(item =>
            item.PaymentProviderId == _paymentProviderId && item.WebhookEventId == "evt_bachs_integration");
        _webhookInboxId = inbox.Id;
        inbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
        inbox.SignatureValidationStatus.ShouldBe(SignatureValidationStatus.Valid);
        var outbox = await dbContext.OutboxMessages.SingleAsync(item =>
            item.Type.Contains(nameof(SuccessfulPaymentConfirmed)) && item.Payload.Contains(MerchantReference));
        _outboxMessageId = outbox.Id;
    }

    private async Task SeedPaymentSetupAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendProjectTemplate.Infrastructure.Persistence.AppDbContext>();
        var provider = PaymentProvider.Create("Bachs", PaymentProviderKeys.Bachs, true);
        var currency = Currency.Create("NGN", "Naira", true);
        var transaction = PaymentTransaction.Create(
            MerchantReference,
            PaymentIntent.WalletTopUp,
            provider.Id,
            2500m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated(CheckoutId, null, null, "payment_initiated");

        await dbContext.PaymentProviders.AddAsync(provider);
        await dbContext.Currencies.AddAsync(currency);
        await dbContext.PaymentTransactions.AddAsync(transaction);
        await dbContext.SaveChangesAsync();

        _paymentProviderId = provider.Id;
        _paymentTransactionId = transaction.Id;
        _currencyId = currency.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendProjectTemplate.Infrastructure.Persistence.AppDbContext>();
        if (_outboxMessageId != Guid.Empty)
        {
            var outbox = await dbContext.OutboxMessages.FindAsync(_outboxMessageId);
            if (outbox is not null)
            {
                dbContext.OutboxMessages.Remove(outbox);
            }
        }

        if (_webhookInboxId != Guid.Empty)
        {
            var inbox = await dbContext.PaymentWebhookInboxes.FindAsync(_webhookInboxId);
            if (inbox is not null)
            {
                dbContext.PaymentWebhookInboxes.Remove(inbox);
            }
        }

        var transaction = await dbContext.PaymentTransactions.FindAsync(_paymentTransactionId);
        if (transaction is not null)
        {
            dbContext.PaymentTransactions.Remove(transaction);
        }

        var provider = await dbContext.PaymentProviders.FindAsync(_paymentProviderId);
        if (provider is not null)
        {
            dbContext.PaymentProviders.Remove(provider);
        }

        var currency = await dbContext.Currencies.FindAsync(_currencyId);
        if (currency is not null)
        {
            dbContext.Currencies.Remove(currency);
        }

        await dbContext.SaveChangesAsync();
    }

    private static string ComputeSignature(long timestamp, string payload) =>
        Convert.ToHexStringLower(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(WebhookSecret),
                Encoding.UTF8.GetBytes($"{timestamp}.{payload}")));
}
