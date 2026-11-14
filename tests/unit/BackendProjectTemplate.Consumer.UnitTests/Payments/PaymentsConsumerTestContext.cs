using BackendProjectTemplate.Consumer.Payments;
using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.UnitTests.Payments;

internal sealed class PaymentsConsumerTestContext
{
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActorAccessor CurrentActorAccessor { get; } = Substitute.For<ICurrentActorAccessor>();
    public IMessageContext MessageContext { get; } = Substitute.For<IMessageContext>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public IRepository<Currency> CurrencyRepository { get; } = Substitute.For<IRepository<Currency>>();
    public IRepository<Wallet> WalletRepository { get; } = Substitute.For<IRepository<Wallet>>();
    public IRepository<WalletTransaction> WalletTransactionRepository { get; } = Substitute.For<IRepository<WalletTransaction>>();
    public IRepository<SubscriptionActivation> SubscriptionActivationRepository { get; } = Substitute.For<IRepository<SubscriptionActivation>>();
    public IRepository<PaymentTransaction> PaymentTransactionRepository { get; } = Substitute.For<IRepository<PaymentTransaction>>();
    public IRepository<SubscriptionPurchase> SubscriptionPurchaseRepository { get; } = Substitute.For<IRepository<SubscriptionPurchase>>();
    public IRepository<SubscriptionAggregate> SubscriptionRepository { get; } = Substitute.For<IRepository<SubscriptionAggregate>>();
    public ISubscriptionLifecyclePersistence SubscriptionLifecyclePersistence { get; } = Substitute.For<ISubscriptionLifecyclePersistence>();
    public IRepository<MessageInbox> MessageInboxRepository { get; } = Substitute.For<IRepository<MessageInbox>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 25, 14, 0, 0, TimeSpan.Zero));
    public ILogger<SuccessfulPaymentConfirmedHandler> SuccessfulPaymentConfirmedLogger { get; } = Substitute.For<ILogger<SuccessfulPaymentConfirmedHandler>>();
    public ILogger<CreditWalletHandler> CreditWalletLogger { get; } = Substitute.For<ILogger<CreditWalletHandler>>();
    public ILogger<ActivateSubscriptionHandler> ActivateSubscriptionLogger { get; } = Substitute.For<ILogger<ActivateSubscriptionHandler>>();

    public SuccessfulPaymentConfirmedHandler CreateSuccessfulPaymentConfirmedHandler() =>
        new(CustomTelemetryContext, CurrentActorAccessor, MessageContext, CommandSender, UnitOfWork, Clock, MessageInboxRepository, SuccessfulPaymentConfirmedLogger);

    public CreditWalletHandler CreateCreditWalletHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            CurrencyRepository,
            WalletRepository,
            WalletTransactionRepository,
            UnitOfWork,
            Clock,
            MessageInboxRepository,
            CreditWalletLogger);

    public ActivateSubscriptionHandler CreateActivateSubscriptionHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            PaymentTransactionRepository,
            SubscriptionPurchaseRepository,
            SubscriptionRepository,
            SubscriptionActivationRepository,
            SubscriptionLifecyclePersistence,
            UnitOfWork,
            Clock,
            MessageInboxRepository,
            ActivateSubscriptionLogger);

    public CreditWalletCommand CreateCreditWalletCommand(decimal amount, Guid currencyId)
    {
        var command = new CreditWalletCommand(Guid.CreateVersion7(), $"pay_{Guid.CreateVersion7():N}", amount, currencyId)
        {
            StakeholderId = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7().ToString("N")
        };

        return command;
    }

    public ActivateSubscriptionCommand CreateActivateSubscriptionCommand(decimal amount, Guid currencyId)
    {
        var command = new ActivateSubscriptionCommand(Guid.CreateVersion7(), $"pay_{Guid.CreateVersion7():N}", amount, currencyId)
        {
            StakeholderId = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7().ToString("N")
        };

        var payment = PaymentTransaction.Create(
            command.MerchantReference,
            Contracts.Payments.PaymentIntent.Subscription,
            Guid.CreateVersion7(),
            amount,
            currencyId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            command.StakeholderId!.Value,
            command.TenantId);
        typeof(Domain.Common.Entities.Entity)
            .GetProperty(nameof(Domain.Common.Entities.Entity.Id))!
            .SetValue(payment, command.PaymentTransactionId);
        payment.MarkInitiated("provider-reference", null, null, "initiated");
        payment.MarkSucceeded("provider-reference", "confirmed", Clock.GetUtcNow());

        var billingAccountId = Guid.CreateVersion7();
        var plan = SubscriptionPlan.Create("TEST_MONTHLY", "Test Monthly", amount, currencyId, BillingInterval.Monthly, true);
        var subscription = SubscriptionAggregate.CreatePending(
            command.TenantId,
            billingAccountId,
            plan.Id,
            true,
            Clock.GetUtcNow(),
            Guid.CreateVersion7(),
            command.StakeholderId.Value.ToString());
        var purchase = SubscriptionPurchase.Create(
            command.TenantId,
            billingAccountId,
            subscription.Id,
            plan,
            payment.Id,
            command.StakeholderId.Value);

        PaymentTransactionRepository.GetByIdAsync(command.PaymentTransactionId, Arg.Any<CancellationToken>()).Returns(payment);
        SubscriptionPurchaseRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SubscriptionPurchase>>(),
                Arg.Any<CancellationToken>())
            .Returns(purchase);
        SubscriptionRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SubscriptionAggregate>>(),
                Arg.Any<CancellationToken>())
            .Returns(subscription);

        return command;
    }

    public void SetCorrelationId() => MessageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

    public Currency CreateCurrency(Guid id, string currencyCode)
    {
        var currency = Currency.Create(currencyCode, currencyCode, true);
        typeof(Domain.Common.Entities.Entity)
            .GetProperty(nameof(Domain.Common.Entities.Entity.Id))!
            .SetValue(currency, id);
        return currency;
    }

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

