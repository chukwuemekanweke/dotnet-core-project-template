using BackendProjectTemplate.Application.Payments.Features.InitiatePayment;
using BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;
using BackendProjectTemplate.Contracts.Events;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using Shouldly;
using SubscriptionAggregate = BackendProjectTemplate.Domain.Subscriptions.Entities.Subscription;

namespace BackendProjectTemplate.Application.UnitTests.Subscriptions.PurchaseSubscription;

public sealed class When_PurchasingSubscription_WithFailedProviderInitiation_Should
{
    [Fact]
    public async Task FailThePaymentReleaseThePurchaseAndReplayTheFailureWithoutCallingTheProviderAgain()
    {
        // Given
        var tenantId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(),
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Ada",
            "Lovelace");
        var currency = Currency.Create("NGN", "Naira", true);
        var plan = SubscriptionPlan.Create("PREMIUM", "Premium", 900000m, currency.Id, BillingInterval.Monthly, true);
        var provider = PaymentProvider.Create("Bachs", "bachs", true);
        provider.SetConfiguration(currency.Id, PaymentIntent.Subscription, PaymentMethodType.PaymentLink, true);
        var billingAccount = BillingAccount.Create(tenantId, BillingAccountOwnerType.Stakeholder, stakeholder.Id, "Ada Lovelace");

        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var currencyRepository = Substitute.For<IRepository<Currency>>();
        var countryCurrencyRepository = Substitute.For<IRepository<CountryCurrency>>();
        var providerRepository = Substitute.For<IRepository<PaymentProvider>>();
        var providerConfigurationRepository = Substitute.For<IRepository<PaymentProviderConfiguration>>();
        var paymentRepository = Substitute.For<IRepository<PaymentTransaction>>();
        var planRepository = Substitute.For<IRepository<SubscriptionPlan>>();
        var purchaseRepository = Substitute.For<IRepository<SubscriptionPurchase>>();
        var billingAccountResolver = Substitute.For<IBillingAccountResolver>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var telemetry = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var providerService = Substitute.For<IPaymentProviderService>();
        providerService.ProviderKey.Returns(provider.ProviderKey);

        stakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);
        currencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>()).Returns(currency);
        countryCurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<CountryCurrency>>(), Arg.Any<CancellationToken>())
            .Returns(CountryCurrency.Create(stakeholder.CountryId, currency.Id, true, true));
        providerRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>()).Returns(provider);
        providerRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        providerConfigurationRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentProviderConfiguration>>(),
                Arg.Any<CancellationToken>())
            .Returns(provider.Configurations.Single());
        planRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns(plan);
        billingAccountResolver.ResolveOrCreateAsync(tenantId, BillingAccountOwnerType.Stakeholder, stakeholder.Id, Arg.Any<CancellationToken>())
            .Returns(billingAccount);
        providerService.InitiatePaymentAsync(Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<PaymentProviderInitiationResult>>(_ => throw new HttpRequestException("Bachs is unavailable."));

        PaymentTransaction? payment = null;
        paymentRepository.AddAsync(Arg.Do<PaymentTransaction>(value => payment = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        SubscriptionPurchase? purchase = null;
        purchaseRepository.AddAsync(Arg.Do<SubscriptionPurchase>(value => purchase = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        UnsuccessfulPaymentConcluded? publishedEvent = null;
        eventPublisher.PublishAsync(Arg.Do<UnsuccessfulPaymentConcluded>(value => publishedEvent = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var clock = new FakeTimeProvider();
        var handler = new PurchaseSubscriptionHandler(
            planRepository,
            Substitute.For<IRepository<SubscriptionAggregate>>(),
            purchaseRepository,
            paymentRepository,
            providerRepository,
            billingAccountResolver,
            new PaymentInitiationService(
                stakeholderRepository,
                currencyRepository,
                countryCurrencyRepository,
                providerRepository,
                providerConfigurationRepository,
                paymentRepository,
                Substitute.For<IRepository<RecurringPaymentAuthorization>>(),
                [providerService],
                eventPublisher,
                telemetry,
                unitOfWork,
                clock),
            telemetry,
            unitOfWork,
            clock);
        var command = new PurchaseSubscriptionCommand(
            plan.Id,
            BillingInterval.Monthly,
            provider.Id,
            BillingAccountOwnerType.Stakeholder,
            false,
            Guid.CreateVersion7(),
            new ActorContext(stakeholder.Id, tenantId, "correlation", "flow"));

        // When
        await Should.ThrowAsync<HttpRequestException>(() => handler.HandleAsync(command, CancellationToken.None));

        // Then: the committed payment is failed and the subscription release is published with it.
        payment.ShouldNotBeNull();
        payment.PaymentStatus.ShouldBe(PaymentStatus.Failed);
        payment.FailureReason.ShouldBe(KnownPaymentTransactionChangeReasons.ProviderInitiationFailed);
        payment.FailedAtUtc.ShouldBe(clock.GetUtcNow());
        publishedEvent.ShouldNotBeNull();
        publishedEvent.PaymentTransactionId.ShouldBe(payment.Id);
        publishedEvent.PaymentStatus.ShouldBe(PaymentStatus.Failed);
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());

        // When: the client retries with the same idempotency key.
        purchase.ShouldNotBeNull();
        purchaseRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPurchase>>(), Arg.Any<CancellationToken>())
            .Returns(purchase);
        paymentRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        var replay = await handler.HandleAsync(command, CancellationToken.None);

        // Then: the replay reports the failure instead of a payment that looks pending forever.
        replay.PurchaseId.ShouldBe(purchase.Id);
        replay.PaymentStatus.ShouldBe(PaymentStatus.Failed);
        await providerService.Received(1).InitiatePaymentAsync(Arg.Any<PaymentProviderInitiationRequest>(), Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    }
}
