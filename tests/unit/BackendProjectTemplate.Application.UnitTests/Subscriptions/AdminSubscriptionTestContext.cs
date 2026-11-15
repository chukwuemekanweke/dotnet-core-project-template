using BackendProjectTemplate.Application.Subscriptions.Features.AdministrativelyCancelSubscriptionAtPeriodEnd;
using BackendProjectTemplate.Application.Subscriptions.Features.AllocateSubscription;
using BackendProjectTemplate.Application.Subscriptions.Features.CancelSubscriptionImmediately;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Microsoft.Extensions.Logging;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions;

internal sealed class AdminSubscriptionTestContext
{
    public static readonly DateTimeOffset Now = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    public AdminSubscriptionTestContext()
    {
        PlanRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns(Plan);
        BillingAccountResolver.ResolveOrCreateAsync(
                TenantId,
                BillingAccountOwnerType.Tenant,
                null,
                Arg.Any<CancellationToken>())
            .Returns(TenantBillingAccount);
    }

    public Guid TenantId { get; } = Guid.CreateVersion7();
    public Guid AdministratorId { get; } = Guid.CreateVersion7();
    public SubscriptionPlan Plan { get; } = SubscriptionPlan.Create("PARTNER", "Partner", 500000m, Guid.CreateVersion7(), BillingInterval.Monthly, true);
    public BillingAccount TenantBillingAccount => _tenantBillingAccount ??= BillingAccount.Create(TenantId, BillingAccountOwnerType.Tenant, null, "Tenant");
    public IRepository<SubscriptionPlan> PlanRepository { get; } = Substitute.For<IRepository<SubscriptionPlan>>();
    public IRepository<SubscriptionAggregate> SubscriptionRepository { get; } = Substitute.For<IRepository<SubscriptionAggregate>>();
    public IRepository<SubscriptionAllocation> AllocationRepository { get; } = Substitute.For<IRepository<SubscriptionAllocation>>();
    public IRepository<SubscriptionRenewal> RenewalRepository { get; } = Substitute.For<IRepository<SubscriptionRenewal>>();
    public IBillingAccountResolver BillingAccountResolver { get; } = Substitute.For<IBillingAccountResolver>();
    public ISubscriptionLifecyclePersistence LifecyclePersistence { get; } = Substitute.For<ISubscriptionLifecyclePersistence>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public ICustomTelemetryContext Telemetry { get; } = Substitute.For<ICustomTelemetryContext>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(Now);

    private BillingAccount? _tenantBillingAccount;

    public ActorContext Actor => new(AdministratorId, Guid.CreateVersion7(), "correlation", "flow");

    public AllocateSubscriptionCommand CreateAllocation(
        DateTimeOffset? startAtUtc = null,
        DateTimeOffset? endAtUtc = null,
        Guid? idempotencyKey = null,
        BillingAccountOwnerType ownerType = BillingAccountOwnerType.Tenant,
        Guid? stakeholderId = null) =>
        new(
            TenantId,
            ownerType,
            stakeholderId,
            Plan.Id,
            startAtUtc,
            endAtUtc,
            SubscriptionAllocationSource.AdminAllocation,
            "Partner onboarding",
            idempotencyKey ?? Guid.CreateVersion7(),
            Actor);

    public AllocateSubscriptionHandler CreateAllocateHandler() =>
        new(
            PlanRepository,
            SubscriptionRepository,
            AllocationRepository,
            BillingAccountResolver,
            CommandSender,
            Telemetry,
            UnitOfWork,
            Clock,
            Substitute.For<ILogger<AllocateSubscriptionHandler>>());

    public CancelSubscriptionImmediatelyHandler CreateImmediateCancellationHandler() =>
        new(
            SubscriptionRepository,
            RenewalRepository,
            LifecyclePersistence,
            Telemetry,
            UnitOfWork,
            Clock,
            Substitute.For<ILogger<CancelSubscriptionImmediatelyHandler>>());

    public AdministrativelyCancelSubscriptionAtPeriodEndHandler CreatePeriodEndCancellationHandler() =>
        new(SubscriptionRepository, LifecyclePersistence, CommandSender, Telemetry, UnitOfWork, Clock);

    public SubscriptionAggregate CreateActiveAllocation(int days = 30) =>
        SubscriptionAggregate.CreateAllocated(
            TenantId,
            TenantBillingAccount.Id,
            Plan,
            Now.AddDays(-1),
            Now.AddDays(days),
            Now.AddDays(-1),
            Guid.CreateVersion7(),
            AdministratorId.ToString(),
            "Seeded allocation");

    public void ReturnSubscription(SubscriptionAggregate subscription) =>
        SubscriptionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionAggregate>>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
