using BackendProjectTemplate.Consumer.Subscriptions;
using BackendProjectTemplate.Contracts.Commands.Subscriptions;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.Extensions.Options;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Consumer.UnitTests.Subscriptions;

internal sealed class SubscriptionsConsumerTestContext
{
    public static readonly DateTimeOffset AllocatedAt = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    public SubscriptionsConsumerTestContext(DateTimeOffset now)
    {
        Clock = new MutableTimeProvider(now);
        MessageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
    }

    public Guid TenantId { get; } = Guid.CreateVersion7();
    public Guid BillingAccountId { get; } = Guid.CreateVersion7();
    public string ActorId { get; } = Guid.CreateVersion7().ToString();
    public SubscriptionPlan Plan { get; } = SubscriptionPlan.Create("ALLOCATED", "Allocated", 250000m, Guid.CreateVersion7(), BillingInterval.Monthly, true);
    public ICustomTelemetryContext Telemetry { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActorAccessor ActorAccessor { get; } = Substitute.For<ICurrentActorAccessor>();
    public IMessageContext MessageContext { get; } = Substitute.For<IMessageContext>();
    public IRepository<SubscriptionAggregate> SubscriptionRepository { get; } = Substitute.For<IRepository<SubscriptionAggregate>>();
    public IRepository<SubscriptionAllocation> AllocationRepository { get; } = Substitute.For<IRepository<SubscriptionAllocation>>();
    public IRepository<SubscriptionRenewal> RenewalRepository { get; } = Substitute.For<IRepository<SubscriptionRenewal>>();
    public IRepository<RecurringPaymentAuthorization> AuthorizationRepository { get; } = Substitute.For<IRepository<RecurringPaymentAuthorization>>();
    public IRepository<PaymentTransaction> PaymentRepository { get; } = Substitute.For<IRepository<PaymentTransaction>>();
    public ISubscriptionLifecyclePersistence LifecyclePersistence { get; } = Substitute.For<ISubscriptionLifecyclePersistence>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IRepository<MessageInbox> InboxRepository { get; } = Substitute.For<IRepository<MessageInbox>>();
    public MutableTimeProvider Clock { get; }

    public (SubscriptionAggregate Subscription, SubscriptionAllocation Allocation) CreateScheduledAllocation(DateTimeOffset start, DateTimeOffset end)
    {
        var operationId = Guid.CreateVersion7();
        var subscription = SubscriptionAggregate.CreateAllocated(
            TenantId, BillingAccountId, Plan, Plan.Prices.Single().BillingInterval, start, end, AllocatedAt, operationId, ActorId, "Scheduled grant");
        var allocation = SubscriptionAllocation.Create(
            subscription, Plan, SubscriptionAllocationSource.Promotion, "Scheduled grant", ActorId, operationId, new string('f', 64));
        SubscriptionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionAggregate>>(), Arg.Any<CancellationToken>())
            .Returns(subscription);
        AllocationRepository.GetByIdAsync(allocation.Id, Arg.Any<CancellationToken>()).Returns(allocation);
        return (subscription, allocation);
    }

    public ActivateAllocatedSubscriptionCommand CreateActivation(SubscriptionAggregate subscription, SubscriptionAllocation allocation) =>
        new(subscription.Id, allocation.Id, allocation.StartAtUtc) { TenantId = TenantId };

    public ActivateAllocatedSubscriptionHandler CreateActivationHandler() =>
        new(
            Telemetry,
            ActorAccessor,
            MessageContext,
            SubscriptionRepository,
            AllocationRepository,
            LifecyclePersistence,
            CommandSender,
            UnitOfWork,
            Clock,
            InboxRepository,
            Substitute.For<ILogger<ActivateAllocatedSubscriptionHandler>>());

    public ExpireSubscriptionHandler CreateExpireHandler() =>
        new(Telemetry, ActorAccessor, MessageContext, SubscriptionRepository, LifecyclePersistence, UnitOfWork, Clock, InboxRepository,
            Substitute.For<ILogger<ExpireSubscriptionHandler>>());

    public RenewSubscriptionHandler CreateRenewHandler()
    {
        var options = Options.Create(new SubscriptionRenewalOptions());
        var charger = new SubscriptionRenewalCharger(
            PaymentRepository,
            Substitute.For<IRepository<PaymentProvider>>(),
            Substitute.For<IRepository<Currency>>(),
            [],
            Substitute.For<IRecurringPaymentAuthorizationProtector>(),
            Substitute.For<IEventPublisher>(),
            CommandSender,
            options,
            UnitOfWork,
            Clock);
        return new RenewSubscriptionHandler(
            Telemetry,
            ActorAccessor,
            MessageContext,
            SubscriptionRepository,
            RenewalRepository,
            AuthorizationRepository,
            PaymentRepository,
            charger,
            CommandSender,
            options,
            UnitOfWork,
            Clock,
            InboxRepository,
            Substitute.For<ILogger<RenewSubscriptionHandler>>());
    }

    internal sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
