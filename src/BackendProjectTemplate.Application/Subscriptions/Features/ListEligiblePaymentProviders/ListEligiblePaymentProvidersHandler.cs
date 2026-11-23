using BackendProjectTemplate.Contracts.Payments;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Services;
using BackendProjectTemplate.Domain.Payments.Specifications;
using BackendProjectTemplate.Domain.Stakeholders.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Entities;
using BackendProjectTemplate.Domain.Subscriptions.Specifications;

namespace BackendProjectTemplate.Application.Subscriptions.Features.ListEligiblePaymentProviders;

public sealed class ListEligiblePaymentProvidersHandler(
    IReadRepository<SubscriptionPlan> planRepository,
    IReadRepository<Stakeholder> stakeholderRepository,
    IReadRepository<CountryCurrency> countryCurrencyRepository,
    IReadRepository<PaymentProviderConfiguration> configurationRepository,
    IReadRepository<PaymentProvider> providerRepository,
    IEnumerable<IRecurringPaymentProvider> recurringPaymentProviders)
{
    public async Task<IReadOnlyList<EligiblePaymentProviderResponse>?> HandleAsync(
        ListEligiblePaymentProvidersCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = command.ActorContext.TenantId
            ?? throw new InvalidOperationException("Authenticated tenant id is required.");
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required.");
        var plan = await planRepository.FirstOrDefaultAsync(
            new ActiveSubscriptionPlanByIdSpecification(command.PlanId),
            cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId, cancellationToken)
            ?? throw new InvalidOperationException($"Stakeholder '{stakeholderId}' was not found.");
        if (stakeholder.TenantId != tenantId)
        {
            throw new InvalidOperationException("The authenticated stakeholder does not belong to the active tenant.");
        }
        var countryCurrency = await countryCurrencyRepository.FirstOrDefaultAsync(
            new CountryCurrencyByCountryAndCurrencySpecification(stakeholder.CountryId, plan.CurrencyId),
            cancellationToken);
        if (countryCurrency is null)
        {
            return [];
        }

        var configurations = await configurationRepository.ListAsync(
            new EnabledPaymentProviderConfigurationsForCurrencySpecification(plan.CurrencyId, PaymentIntent.Subscription),
            cancellationToken);
        if (configurations.Count == 0)
        {
            return [];
        }

        var providers = await providerRepository.ListAsync(
            new ActivePaymentProvidersByIdsSpecification(configurations.Select(item => item.PaymentProviderId).ToArray()),
            cancellationToken);
        var recurringKeys = recurringPaymentProviders
            .Select(provider => provider.ProviderKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var configurationByProvider = configurations.ToDictionary(item => item.PaymentProviderId);

        return providers
            .OrderBy(provider => provider.ProviderName, StringComparer.OrdinalIgnoreCase)
            .Select(provider => new EligiblePaymentProviderResponse(
                provider.Id,
                provider.ProviderName,
                configurationByProvider[provider.Id].PaymentMethodType.ToString(),
                recurringKeys.Contains(provider.ProviderKey)))
            .ToArray();
    }
}
