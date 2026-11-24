using BackendProjectTemplate.Application.Payments.Common;
using BackendProjectTemplate.Domain.Common.Auditing;
using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Messaging;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.Payments.Features.InitiatePayment;

public sealed class PaymentInitiationService(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<Currency> currencyRepository,
    IRepository<CountryCurrency> countryCurrencyRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentProviderConfiguration> paymentProviderConfigurationRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<RecurringPaymentAuthorization> recurringAuthorizationRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    IEventPublisher eventPublisher,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<InitiatePaymentResult> InitiateAsync(
        InitiatePaymentCommand command,
        Func<PaymentTransaction, CancellationToken, Task>? persistCorrelation,
        CancellationToken cancellationToken)
    {
        if (command.Amount <= 0 || decimal.Truncate(command.Amount) != command.Amount)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Payment amount must be a positive whole number of minor currency units.");
        }

        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required to initiate a payment.");

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken)
            ?? throw new InvalidOperationException($"Unable to resolve stakeholder '{stakeholderId}' for payment initiation.");

        var currency = await currencyRepository.FirstOrDefaultAsync(
            new ActiveCurrencyByIdSpecification(command.CurrencyId),
            cancellationToken)
            ?? throw new InvalidOperationException($"Currency '{command.CurrencyId}' is not active.");

        var supportedCountryCurrency = await countryCurrencyRepository.FirstOrDefaultAsync(
            new CountryCurrencyByCountryAndCurrencySpecification(stakeholder.CountryId, command.CurrencyId),
            cancellationToken);
        if (supportedCountryCurrency is null)
        {
            throw new InvalidOperationException(
                $"Currency '{currency.CurrencyCode}' is not supported for country '{stakeholder.CountryId}'.");
        }

        var paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
                new ActivePaymentProviderByIdSpecification(command.PaymentProviderId),
                cancellationToken)
            ?? throw new InvalidOperationException($"Payment provider '{command.PaymentProviderId}' is not active.");

        _ = await paymentProviderConfigurationRepository.FirstOrDefaultAsync(
                new EnabledPaymentProviderConfigurationSpecification(command.PaymentProviderId, command.CurrencyId, command.PaymentIntent),
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Payment provider '{paymentProvider.ProviderName}' does not support '{currency.CurrencyCode}' for '{command.PaymentIntent}'.");

        var paymentProviderService = paymentProviderServices.SingleOrDefault(service =>
                string.Equals(service.ProviderKey, paymentProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new PaymentProviderResolutionException(
                $"No payment provider service is registered for '{paymentProvider.ProviderKey}'.");

        var paymentTransaction = PaymentTransaction.Create(
            $"pay_{Guid.CreateVersion7():N}",
            command.PaymentIntent,
            paymentProvider.Id,
            command.Amount,
            command.CurrencyId,
            stakeholder.CountryId,
            stakeholder.AppUserId,
            stakeholder.Id,
            stakeholder.TenantId);
        await paymentTransactionRepository.AddAsync(paymentTransaction, cancellationToken);

        if (persistCorrelation is not null)
        {
            await persistCorrelation(paymentTransaction, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        PaymentProviderInitiationResult initiationResult;
        try
        {
            initiationResult = await paymentProviderService.InitiatePaymentAsync(
                new PaymentProviderInitiationRequest(
                    paymentTransaction.MerchantReference,
                    command.Amount,
                    currency.CurrencyCode,
                    command.PaymentIntent,
                    stakeholder.Id,
                    stakeholder.TenantId,
                    stakeholder.CountryId,
                    command.TrustedSubscriptionContext is not null,
                    command.TrustedSubscriptionContext?.SavePaymentMethod == true),
                cancellationToken);
        }
        catch (Exception exception) when (persistCorrelation is not null)
        {
            // The payment and its correlation are already committed, so an idempotent replay would otherwise
            // return a payment with no checkout that never concludes. The customer was given nothing to pay, so
            // the payment can never succeed: fail it so the correlated purchase is released.
            await FailUninitiatedPaymentAsync(paymentTransaction, paymentProvider, command.ActorContext, exception);
            throw;
        }

        if (initiationResult.RecurringPaymentSetup is not null)
        {
            var trustedContext = command.TrustedSubscriptionContext
                ?? throw new InvalidOperationException("A provider returned recurring setup details outside the trusted subscription flow.");
            if (!trustedContext.SavePaymentMethod || !trustedContext.ConsentedAtUtc.HasValue)
            {
                throw new InvalidOperationException("Recurring payment setup requires explicit customer consent.");
            }

            await recurringAuthorizationRepository.AddAsync(
                RecurringPaymentAuthorization.Create(
                    stakeholder.TenantId,
                    trustedContext.BillingAccountId,
                    paymentProvider.Id,
                    initiationResult.RecurringPaymentSetup.ProviderCustomerReference,
                    currency.Id,
                    paymentTransaction.Id,
                    trustedContext.ConsentedAtUtc.Value,
                    trustedContext.ConsentedAtUtc.Value),
                cancellationToken);
        }

        paymentTransaction.MarkInitiated(
            initiationResult.ProviderReference,
            initiationResult.InstructionFields.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
            initiationResult.ExpiresAtUtc,
            KnownPaymentTransactionChangeReasons.PaymentInitiated);
        paymentTransaction.SetPaymentMethodType(initiationResult.PaymentMethodType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.Initiated,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                stakeholder.Id,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.Provider] = paymentProvider.ProviderKey,
                    [Observability.PropertyNames.Payments.PaymentMethod] = paymentTransaction.PaymentMethodType.ToString(),
                    [Observability.PropertyNames.Payments.PaymentIntent] = paymentTransaction.PaymentIntent.ToString(),
                    [Observability.PropertyNames.Payments.MerchantReference] = paymentTransaction.MerchantReference,
                    [Observability.PropertyNames.Payments.ProviderReference] = paymentTransaction.ProviderReference ?? string.Empty,
                    [Observability.PropertyNames.Payments.CurrencyCode] = currency.CurrencyCode
                }));

        return new InitiatePaymentResult(
            paymentTransaction.Id,
            paymentTransaction.MerchantReference,
            paymentTransaction.PaymentStatus,
            paymentProvider.Id,
            paymentProvider.ProviderName,
            paymentTransaction.ExpiresAtUtc,
            paymentTransaction.PaymentMethodType,
            initiationResult.InstructionFields);
    }

    private async Task FailUninitiatedPaymentAsync(
        PaymentTransaction paymentTransaction,
        PaymentProvider paymentProvider,
        ActorContext actorContext,
        Exception providerException)
    {
        // Uses CancellationToken.None so a cancelled request still records the failure instead of leaving the
        // committed payment PendingInitiation.
        try
        {
            var now = timeProvider.GetUtcNow();
            paymentTransaction.MarkInitiationFailed(
                KnownPaymentTransactionChangeReasons.ProviderInitiationFailed,
                KnownPaymentTransactionChangeReasons.ProviderInitiationFailed,
                now);
            await UnsuccessfulPaymentNotification.PublishAsync(paymentTransaction, eventPublisher, now, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception cleanupException)
        {
            throw new AggregateException(
                $"Payment '{paymentTransaction.MerchantReference}' could not be initiated and could not be marked failed.",
                providerException,
                cleanupException);
        }

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.InitiationFailed,
            ObservabilityEventProperties.Create(
                actorContext,
                paymentTransaction.StakeholderId,
                failureReason: providerException.GetType().Name,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.Provider] = paymentProvider.ProviderKey,
                    [Observability.PropertyNames.Payments.PaymentIntent] = paymentTransaction.PaymentIntent.ToString(),
                    [Observability.PropertyNames.Payments.MerchantReference] = paymentTransaction.MerchantReference
                }));
    }
}
