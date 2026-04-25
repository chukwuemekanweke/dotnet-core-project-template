using System.Text.Json;
using BackendProjectTemplate.Application;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Infrastructure.Messaging;
using BackendProjectTemplate.Infrastructure.Payments;
using BackendProjectTemplate.Jobs.IntegrationTests.Infrastructure;
using BackendProjectTemplate.Jobs.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BackendProjectTemplate.Jobs.IntegrationTests.Payments.Reconciliation;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReconcilingPendingPayments_WithSuccessfulVerification_Should(ContainersFixture fixture)
    : JobsWorkerIntegrationTestBase(fixture)
{
    private Guid _paymentProviderId;
    private Guid _currencyId;
    private Guid _paymentTransactionId;
    private Guid _outboxMessageId;

    [Fact]
    public async Task MarkTransactionAsSucceededAndQueueEvent()
    {
        await WhenTheReconciliationWorkerRuns();
        await ThenThePaymentIsSucceededAndEventIsQueued();

        async Task WhenTheReconciliationWorkerRuns()
        {
            await WaitForConditionAsync(async () =>
            {
                await using var scope = CreateDbContextScope();
                var transaction = await scope.DbContext.PaymentTransactions.FindAsync(_paymentTransactionId);
                return transaction?.PaymentStatus == Contracts.Payments.PaymentStatus.Succeeded;
            });
        }

        async Task ThenThePaymentIsSucceededAndEventIsQueued()
        {
            await using var scope = CreateDbContextScope();
            var transaction = await scope.DbContext.PaymentTransactions.FindAsync(_paymentTransactionId);
            transaction.ShouldNotBeNull();
            transaction.PaymentStatus.ShouldBe(Contracts.Payments.PaymentStatus.Succeeded);

            var outboxMessage = scope.DbContext.OutboxMessages
                .Where(message =>
                    message.Kind == OutboxMessageKind.Event &&
                    message.Type == typeof(SuccessfulPaymentConfirmed).FullName! &&
                    message.Payload.Contains(_paymentTransactionId.ToString()))
                .OrderByDescending(message => message.EnqueuedAtUtc)
                .FirstOrDefault();

            outboxMessage.ShouldNotBeNull();
            _outboxMessageId = outboxMessage.Id;

            var @event = JsonSerializer.Deserialize<SuccessfulPaymentConfirmed>(outboxMessage.Payload);
            @event.ShouldNotBeNull();
            @event.PaymentTransactionId.ShouldBe(_paymentTransactionId);
        }
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    protected override async Task InitializeWorkerTestAsync()
    {
        await SeedPendingPaymentAsync();
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        await using var scope = CreateDbContextScope();
        var outboxMessage = await scope.DbContext.OutboxMessages.FindAsync(_outboxMessageId);
        if (outboxMessage is not null)
        {
            scope.DbContext.OutboxMessages.Remove(outboxMessage);
        }

        var transaction = await scope.DbContext.PaymentTransactions.FindAsync(_paymentTransactionId);
        if (transaction is not null)
        {
            scope.DbContext.PaymentTransactions.Remove(transaction);
        }

        var currency = await scope.DbContext.Currencies.FindAsync(_currencyId);
        if (currency is not null)
        {
            scope.DbContext.Currencies.Remove(currency);
        }

        var provider = await scope.DbContext.PaymentProviders.FindAsync(_paymentProviderId);
        if (provider is not null)
        {
            scope.DbContext.PaymentProviders.Remove(provider);
        }

        await scope.DbContext.SaveChangesAsync();
    }

    protected override void RegisterWorkers(IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddPaymentServices(configuration);
        services.AddTransactionalOutbox();
        services.AddPaymentReconciliation(configuration);
    }

    private async Task SeedPendingPaymentAsync()
    {
        await using var scope = CreateDbContextScope();
        var now = TimeProvider.System.GetUtcNow();
        var provider = PaymentProvider.Create("Credo", "credo", true, now);
        var currency = Currency.Create("NGN", "Naira", true, now);
        var transaction = PaymentTransaction.Create(
            "merchant-success",
            Contracts.Payments.PaymentIntent.WalletTopUp,
            provider.Id,
            1500m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            now);
        transaction.MarkInitiated("provider-ref", null, null, "payment_initiated");
        transaction.RecordStatusCheck(now.AddMinutes(-10));

        await scope.DbContext.PaymentProviders.AddAsync(provider);
        await scope.DbContext.Currencies.AddAsync(currency);
        await scope.DbContext.PaymentTransactions.AddAsync(transaction);
        await scope.DbContext.SaveChangesAsync();

        _paymentProviderId = provider.Id;
        _currencyId = currency.Id;
        _paymentTransactionId = transaction.Id;
    }
}
