using BackendProjectTemplate.Application.Payments.Features.ActivatePaymentProvider;
using BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BackendProjectTemplate.Application.Payments.Features.GetStakeholderWalletTransactions;
using BackendProjectTemplate.Application.Payments.Features.InitiatePayment;
using BackendProjectTemplate.Application.Payments.Features.ListPaymentProviders;
using BackendProjectTemplate.Application.Payments.Features.ProcessBachsWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessCredoWebhook;
using BackendProjectTemplate.Application.Payments.Features.ProcessSafeHavenWebhook;
using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.ReadModels;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.WebAPI.Features.Payments.Providers;

namespace BackendProjectTemplate.WebAPI.UnitTests.Features.Payments;

internal sealed class PaymentsControllerTestContext
{
    public IRepository<Stakeholder> StakeholderRepository { get; } = Substitute.For<IRepository<Stakeholder>>();
    public IRepository<Currency> CurrencyRepository { get; } = Substitute.For<IRepository<Currency>>();
    public IRepository<CountryCurrency> CountryCurrencyRepository { get; } = Substitute.For<IRepository<CountryCurrency>>();
    public IRepository<PaymentProvider> PaymentProviderRepository { get; } = Substitute.For<IRepository<PaymentProvider>>();
    public IReadRepository<PaymentProvider> PaymentProviderReadRepository { get; } = Substitute.For<IReadRepository<PaymentProvider>>();
    public IRepository<PaymentProviderConfiguration> PaymentProviderConfigurationRepository { get; } = Substitute.For<IRepository<PaymentProviderConfiguration>>();
    public IRepository<PaymentTransaction> PaymentTransactionRepository { get; } = Substitute.For<IRepository<PaymentTransaction>>();
    public IRepository<PaymentWebhookInbox> PaymentWebhookInboxRepository { get; } = Substitute.For<IRepository<PaymentWebhookInbox>>();
    public IRepository<RecurringPaymentAuthorization> RecurringPaymentAuthorizationRepository { get; } = Substitute.For<IRepository<RecurringPaymentAuthorization>>();
    public IWalletTransactionReadModelRepository WalletTransactionReadModelRepository { get; } = Substitute.For<IWalletTransactionReadModelRepository>();
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActor CurrentActor { get; } = Substitute.For<ICurrentActor>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IAdministrativeAuditRecorder AuditRecorder { get; } = Substitute.For<IAdministrativeAuditRecorder>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public IBachsWebhookSignatureValidator BachsWebhookSignatureValidator { get; } = Substitute.For<IBachsWebhookSignatureValidator>();
    public IRecurringPaymentAuthorizationProtector RecurringPaymentAuthorizationProtector { get; } = Substitute.For<IRecurringPaymentAuthorizationProtector>();
    public ICredoWebhookSignatureValidator CredoWebhookSignatureValidator { get; } = Substitute.For<ICredoWebhookSignatureValidator>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 25, 13, 0, 0, TimeSpan.Zero));
    public List<IPaymentProviderService> PaymentProviderServices { get; } = [];

    public InitiatePaymentHandler CreateInitiatePaymentHandler() =>
        new(new PaymentInitiationService(
            StakeholderRepository,
            CurrencyRepository,
            CountryCurrencyRepository,
            PaymentProviderRepository,
            PaymentProviderConfigurationRepository,
            PaymentTransactionRepository,
            RecurringPaymentAuthorizationRepository,
            PaymentProviderServices,
            EventPublisher,
            CustomTelemetryContext,
            UnitOfWork,
            Clock));

    public ActivatePaymentProviderHandler CreateActivatePaymentProviderHandler() =>
        new(PaymentProviderRepository, AuditRecorder, UnitOfWork);

    public PaymentProvidersController CreatePaymentProvidersController()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());
        currentActor.CorrelationId.Returns("correlation");
        currentActor.FlowId.Returns("flow");
        return new PaymentProvidersController(
            CreateActivatePaymentProviderHandler(),
            CreateListPaymentProvidersHandler(),
            new SetPaymentProviderActivationValidator(),
            currentActor);
    }

    public ListPaymentProvidersHandler CreateListPaymentProvidersHandler() =>
        new(PaymentProviderReadRepository);

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

    public ProcessBachsWebhookHandler CreateBachsWebhookHandler() =>
        new(
            PaymentProviderRepository,
            PaymentWebhookInboxRepository,
            PaymentTransactionRepository,
            RecurringPaymentAuthorizationRepository,
            CurrencyRepository,
            BachsWebhookSignatureValidator,
            RecurringPaymentAuthorizationProtector,
            PaymentProviderServices,
            EventPublisher,
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

    public Stakeholder CreateStakeholder(Guid appUserId, Guid tenantId, Guid countryId) =>
        Stakeholder.Create(appUserId, tenantId, countryId, Guid.CreateVersion7(), "Ada", "Lovelace");

    public Currency CreateCurrency(string currencyCode) =>
        Currency.Create(currencyCode, currencyCode, true);

    public CountryCurrency CreateCountryCurrency(Guid countryId, Guid currencyId, bool isDefault = true, bool isActive = true) =>
        CountryCurrency.Create(countryId, currencyId, isDefault, isActive);

    public PaymentProvider CreatePaymentProvider(string providerName, string providerKey, bool isActive = true) =>
        PaymentProvider.Create(providerName, providerKey, isActive);

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
