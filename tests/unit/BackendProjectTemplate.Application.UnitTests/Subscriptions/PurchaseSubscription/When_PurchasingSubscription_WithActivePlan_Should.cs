using BackendProjectTemplate.Application.Payments.Features.InitiatePayment;
using BackendProjectTemplate.Application.Subscriptions.Features.PurchaseSubscription;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Auditing;
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

public sealed class When_PurchasingSubscription_WithActivePlan_Should
{
    [Fact]
    public async Task DerivePriceAndCurrencyFromPlanAndPersistCorrelationBeforeProviderCall()
    {
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
        var provider = PaymentProvider.Create("Provider", "provider", true);
        provider.SetConfiguration(currency.Id, PaymentIntent.Subscription, PaymentMethodType.PaymentLink, true);
        var billingAccount = BillingAccount.Create(
            tenantId,
            BillingAccountOwnerType.Stakeholder,
            stakeholder.Id,
            "Ada Lovelace");

        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var currencyRepository = Substitute.For<IRepository<Currency>>();
        var countryCurrencyRepository = Substitute.For<IRepository<CountryCurrency>>();
        var providerRepository = Substitute.For<IRepository<PaymentProvider>>();
        var providerConfigurationRepository = Substitute.For<IRepository<PaymentProviderConfiguration>>();
        var paymentRepository = Substitute.For<IRepository<PaymentTransaction>>();
        var planRepository = Substitute.For<IRepository<SubscriptionPlan>>();
        var subscriptionRepository = Substitute.For<IRepository<SubscriptionAggregate>>();
        var purchaseRepository = Substitute.For<IRepository<SubscriptionPurchase>>();
        var billingAccountResolver = Substitute.For<IBillingAccountResolver>();
        var telemetry = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var providerService = Substitute.For<IPaymentProviderService>();
        providerService.ProviderKey.Returns(provider.ProviderKey);

        stakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>()).Returns(stakeholder);
        currencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Currency>>(), Arg.Any<CancellationToken>()).Returns(currency);
        countryCurrencyRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<CountryCurrency>>(), Arg.Any<CancellationToken>())
            .Returns(CountryCurrency.Create(stakeholder.CountryId, currency.Id, true, true));
        providerRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>()).Returns(provider);
        providerConfigurationRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<PaymentProviderConfiguration>>(),
                Arg.Any<CancellationToken>())
            .Returns(provider.Configurations.Single());
        planRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionPlan>>(), Arg.Any<CancellationToken>()).Returns(plan);
        billingAccountResolver.ResolveOrCreateAsync(
                tenantId,
                BillingAccountOwnerType.Stakeholder,
                stakeholder.Id,
                Arg.Any<CancellationToken>())
            .Returns(billingAccount);

        PaymentProviderInitiationRequest? providerRequest = null;
        providerService.InitiatePaymentAsync(
                Arg.Do<PaymentProviderInitiationRequest>(request => providerRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderInitiationResult(
                "provider-reference",
                provider.ProviderName,
                PaymentMethodType.PaymentLink,
                null,
                new Dictionary<string, string>()));

        SubscriptionPurchase? capturedPurchase = null;
        purchaseRepository.AddAsync(
                Arg.Do<SubscriptionPurchase>(purchase => capturedPurchase = purchase),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var paymentInitiationService = new PaymentInitiationService(
            stakeholderRepository,
            currencyRepository,
            countryCurrencyRepository,
            providerRepository,
            providerConfigurationRepository,
            paymentRepository,
            [providerService],
            telemetry,
            unitOfWork);
        var handler = new PurchaseSubscriptionHandler(
            planRepository,
            subscriptionRepository,
            purchaseRepository,
            billingAccountResolver,
            paymentInitiationService,
            telemetry,
            new FakeTimeProvider());

        await handler.HandleAsync(
            new PurchaseSubscriptionCommand(
                plan.Id,
                provider.Id,
                BillingAccountOwnerType.Stakeholder,
                true,
                new ActorContext(stakeholder.Id, tenantId, "correlation", "flow")),
            CancellationToken.None);

        providerRequest.ShouldNotBeNull();
        providerRequest.Amount.ShouldBe(plan.Price);
        providerRequest.CurrencyCode.ShouldBe(currency.CurrencyCode);
        capturedPurchase.ShouldNotBeNull();
        capturedPurchase.ExpectedAmount.ShouldBe(plan.Price);
        capturedPurchase.ExpectedCurrencyId.ShouldBe(plan.CurrencyId);
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    }
}
