using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Providers.Entities;

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

        var activeProviders = providers.Where(provider => provider.Id != selectedProvider.Id && provider.IsActive).ToList();
        foreach (var provider in activeProviders)
        {
            provider.SetActive(false);
            providerRepository.Update(provider);
        }

        if (activeProviders.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (!selectedProvider.IsActive)
        {
            selectedProvider.SetActive(true);
            providerRepository.Update(selectedProvider);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        return new ActivateProviderResult(ActivateProviderStatus.Success);
    }
}
