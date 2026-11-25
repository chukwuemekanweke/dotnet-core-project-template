using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Payments.Entities;
using BackendProjectTemplate.Domain.Payments.Specifications;

namespace BackendProjectTemplate.Application.Payments.Features.ListPaymentProviders;

/// <summary>
/// Lists every visible payment provider with its administrative activation state. <c>IsActive</c> is
/// the persisted administrative switch, not measured provider health. Reads are uncached so activation
/// changes are visible on the next request.
/// </summary>
public sealed class ListPaymentProvidersHandler(IReadRepository<PaymentProvider> paymentProviderRepository)
{
    public async Task<IReadOnlyList<PaymentProviderResponse>> HandleAsync(CancellationToken cancellationToken) =>
        (await paymentProviderRepository.ListAsync(new PaymentProvidersByNameSpecification(), cancellationToken))
            .Select(provider => new PaymentProviderResponse(
                provider.Id,
                provider.ProviderName,
                provider.ProviderKey,
                provider.IsActive))
            .ToArray();
}
