using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Entities;
using BackendProjectTemplate.Domain.Notifications.Specifications;

namespace BackendProjectTemplate.Application.Providers.Features.ActivateProvider;

public sealed class ActivateProviderHandler(
    IRepository<Provider> providerRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ActivateProviderResult> HandleAsync(ActivateProviderCommand command, CancellationToken cancellationToken)
    {
        var providers = await providerRepository.ListAsync(
            new ProvidersByTypeSpecification(command.ProviderType),
            cancellationToken);

        var selectedProvider = providers.SingleOrDefault(provider =>
            string.Equals(provider.ProviderKey, command.ProviderKey, StringComparison.OrdinalIgnoreCase));
        if (selectedProvider is null)
        {
            return new ActivateProviderResult(
                ActivateProviderStatus.ProviderNotFound,
                $"No provider with key '{command.ProviderKey}' was found for type '{command.ProviderType}'.");
        }

        var utcNow = timeProvider.GetUtcNow();
        foreach (var provider in providers)
        {
            var shouldBeActive = provider.Id == selectedProvider.Id;
            if (provider.IsActive == shouldBeActive)
            {
                continue;
            }

            provider.SetActive(shouldBeActive, utcNow);
            providerRepository.Update(provider);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ActivateProviderResult(ActivateProviderStatus.Success);
    }
}
