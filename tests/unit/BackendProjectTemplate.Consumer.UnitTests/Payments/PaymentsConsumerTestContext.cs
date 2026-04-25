using BackendProjectTemplate.Contracts.Commands.Payments;
using BackendProjectTemplate.Consumer.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using NSubstitute;

namespace BackendProjectTemplate.Consumer.UnitTests.Payments;

internal sealed class PaymentsConsumerTestContext
{
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActorAccessor CurrentActorAccessor { get; } = Substitute.For<ICurrentActorAccessor>();
    public IMessageContext MessageContext { get; } = Substitute.For<IMessageContext>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public IRepository<Wallet> WalletRepository { get; } = Substitute.For<IRepository<Wallet>>();
    public IRepository<WalletTransaction> WalletTransactionRepository { get; } = Substitute.For<IRepository<WalletTransaction>>();
    public IRepository<SubscriptionActivation> SubscriptionActivationRepository { get; } = Substitute.For<IRepository<SubscriptionActivation>>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 25, 14, 0, 0, TimeSpan.Zero));

    public SuccessfulPaymentConfirmedHandler CreateSuccessfulPaymentConfirmedHandler() =>
        new(CustomTelemetryContext, CurrentActorAccessor, MessageContext, CommandSender);

    public CreditWalletHandler CreateCreditWalletHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            WalletRepository,
            WalletTransactionRepository,
            UnitOfWork,
            Clock);

    public ActivateSubscriptionHandler CreateActivateSubscriptionHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            SubscriptionActivationRepository,
            UnitOfWork,
            Clock);

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

        return command;
    }

    public void SetCorrelationId() => MessageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
