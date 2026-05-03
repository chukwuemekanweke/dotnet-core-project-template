using BackendProjectTemplate.Domain.Common.Exceptions;
using BackendProjectTemplate.Domain.Common.Observability;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Payments;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Application.Payments.Features.InitiatePayment;

public sealed class InitiatePaymentHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<Currency> currencyRepository,
    IRepository<CountryCurrency> countryCurrencyRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentProviderConfiguration> paymentProviderConfigurationRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<InitiatePaymentResult> HandleAsync(InitiatePaymentCommand command, CancellationToken cancellationToken)
    {
        PaymentProvider? paymentProvider = null;
        PaymentMethodType? paymentMethodType = null;
        string? merchantReference = null;

        try
        {
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

            paymentProvider = await paymentProviderRepository.FirstOrDefaultAsync(
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

            var now = timeProvider.GetUtcNow();

            merchantReference = $"pay_{Guid.CreateVersion7():N}";

            var paymentTransaction = PaymentTransaction.Create(
                merchantReference,
                command.PaymentIntent,
                paymentProvider.Id,
                command.Amount,
                command.CurrencyId,
                stakeholder.CountryId,
                stakeholder.AppUserId,
                stakeholder.Id,
                stakeholder.TenantId,
                now);

            await paymentTransactionRepository.AddAsync(paymentTransaction, cancellationToken);

            var initiationResult = await paymentProviderService.InitiatePaymentAsync(
                new PaymentProviderInitiationRequest(
                    merchantReference,
                    command.Amount,
                    currency.CurrencyCode,
                    command.PaymentIntent,
                    stakeholder.Id,
                    stakeholder.TenantId,
                    stakeholder.CountryId),
                cancellationToken);

            paymentTransaction.MarkInitiated(
                initiationResult.ProviderReference,
                initiationResult.InstructionFields.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
                initiationResult.ExpiresAtUtc,
                KnownPaymentTransactionChangeReasons.PaymentInitiated);
            paymentTransaction.SetPaymentMethodType(initiationResult.PaymentMethodType);
            paymentMethodType = initiationResult.PaymentMethodType;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.Initiated,
                ObservabilityEventProperties.Create(
                    command.ActorContext,
                    stakeholder.Id,
                    additionalProperties: new Dictionary<string, string>
                    {
                        [Observability.ProviderPropertyName] = paymentProvider.ProviderKey,
                        [Observability.PaymentReferencePropertyName] = paymentTransaction.MerchantReference,
                        [Observability.PaymentMethodPropertyName] = paymentTransaction.PaymentMethodType.ToString(),
                        [Observability.PaymentIntentPropertyName] = paymentTransaction.PaymentIntent.ToString(),
                        [Observability.CurrencyIdPropertyName] = paymentTransaction.CurrencyId.ToString()
                    }));

            return new InitiatePaymentResult(
                paymentTransaction.MerchantReference,
                paymentTransaction.PaymentStatus,
                paymentProvider.Id,
                paymentProvider.ProviderName,
                paymentTransaction.ExpiresAtUtc,
                paymentTransaction.PaymentMethodType,
                initiationResult.InstructionFields);
        }
        catch (Exception ex)
        {
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Payments.InitiationFailed,
                ObservabilityEventProperties.Create(
                    command.ActorContext,
                    command.ActorContext.StakeholderId,
                    ex.GetType().Name,
                    new Dictionary<string, string>
                    {
                        [Observability.PaymentIntentPropertyName] = command.PaymentIntent.ToString(),
                        [Observability.CurrencyIdPropertyName] = command.CurrencyId.ToString(),
                        [Observability.ExceptionTypePropertyName] = ex.GetType().Name,
                        [Observability.PaymentReferencePropertyName] = merchantReference ?? string.Empty,
                        [Observability.ProviderPropertyName] = paymentProvider?.ProviderKey ?? string.Empty,
                        [Observability.PaymentMethodPropertyName] = paymentMethodType?.ToString() ?? string.Empty
                    }.Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
                        .ToDictionary(entry => entry.Key, entry => entry.Value)));
            throw;
        }
    }
}
