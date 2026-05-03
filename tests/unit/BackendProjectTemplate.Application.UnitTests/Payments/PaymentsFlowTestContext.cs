using BackendProjectTemplate.Application.Payments.Features.ActivatePaymentProvider;
using BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTransactions;
using BackendProjectTemplate.Application.Payments.Features.InitiatePayment;
using BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook;
using BackendProjectTemplate.Application.Payments.Features.ReconcilePayments;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using NSubstitute;

namespace BackendProjectTemplate.Application.UnitTests.Payments;

internal sealed class PaymentsFlowTestContext
{
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IRepository<Currency> CurrencyRepository { get; } = Substitute.For<IRepository<Currency>>();
    public IRepository<CountryCurrency> CountryCurrencyRepository { get; } = Substitute.For<IRepository<CountryCurrency>>();
    public IRepository<PaymentProvider> PaymentProviderRepository { get; } = Substitute.For<IRepository<PaymentProvider>>();
    public IRepository<PaymentProviderConfiguration> PaymentProviderConfigurationRepository { get; } = Substitute.For<IRepository<PaymentProviderConfiguration>>();
    public IRepository<PaymentTransaction> PaymentTransactionRepository { get; } = Substitute.For<IRepository<PaymentTransaction>>();
    public IRepository<PaymentWebhookInbox> PaymentWebhookInboxRepository { get; } = Substitute.For<IRepository<PaymentWebhookInbox>>();
    public IWalletTransactionReadModelRepository WalletTransactionReadModelRepository { get; } = Substitute.For<IWalletTransactionReadModelRepository>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public ICredoWebhookSignatureValidator CredoWebhookSignatureValidator { get; } = Substitute.For<ICredoWebhookSignatureValidator>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero));
    public List<IPaymentProviderService> PaymentProviderServices { get; } = [];

    public ActivatePaymentProviderHandler CreateActivatePaymentProviderHandler() =>
        new(PaymentProviderRepository, UnitOfWork);

    public InitiatePaymentHandler CreateInitiatePaymentHandler() =>
        new(
            StakeholderRepository,
            CurrencyRepository,
            CountryCurrencyRepository,
            PaymentProviderRepository,
            PaymentProviderConfigurationRepository,
            PaymentTransactionRepository,
            PaymentProviderServices,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public GetStakeholderWalletTransactionsHandler CreateGetStakeholderWalletTransactionsHandler() =>
        new(WalletTransactionReadModelRepository);

    public GetStakeholderWalletTopUpTransactionDetailHandler CreateGetStakeholderWalletTopUpTransactionDetailHandler() =>
        new(WalletTransactionReadModelRepository);

    public ProcessCredoWebhookHandler CreateCredoWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            CredoWebhookSignatureValidator,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public ProcessSafeHavenAccountCreditWebhookHandler CreateSafeHavenAccountCreditWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public ProcessSafeHavenAccountDebitWebhookHandler CreateSafeHavenAccountDebitWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public ProcessSafeHavenVirtualAccountTransferWebhookHandler CreateSafeHavenVirtualAccountTransferWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public PaymentReconciliationService CreatePaymentReconciliationService() =>
        new(
            PaymentTransactionRepository,
            CurrencyRepository,
            PaymentProviderRepository,
            PaymentProviderServices,
            EventPublisher,
            CustomTelemetryContext,
            UnitOfWork,
            Clock);

    public Stakeholder CreateStakeholder(Guid appUserId, Guid tenantId, Guid countryId) =>
        Stakeholder.Create(
            appUserId,
            tenantId,
            countryId,
            Guid.CreateVersion7(),
            "Ada",
            "Lovelace",
            Clock.GetUtcNow());

    public Currency CreateCurrency(string currencyCode) =>
        Currency.Create(currencyCode, currencyCode, true, Clock.GetUtcNow());

    public CountryCurrency CreateCountryCurrency(Guid countryId, Guid currencyId) =>
        CountryCurrency.Create(countryId, currencyId, true, true, Clock.GetUtcNow());

    public PaymentProvider CreatePaymentProvider(string providerName, string providerKey, bool isActive = true) =>
        PaymentProvider.Create(providerName, providerKey, isActive, Clock.GetUtcNow());

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
