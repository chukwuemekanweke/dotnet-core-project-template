using BackendProjectTemplate.Domain.Auditing;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Providers.Entities;

namespace BackendProjectTemplate.Application.Providers.Features.ActivateProvider;

/// <summary>
/// Makes one provider the active implementation for its type. The partial unique index on the active provider
/// per type requires deactivations to be persisted before the activation, so the two saves run inside one
/// explicit transaction: the switch and its administrative audit records commit together or not at all.
/// Each provider whose state changes gets its own audit record; the records share the request correlation.
/// </summary>
public sealed class ActivateProviderHandler(
    IRepository<Provider> providerRepository,
    IAdministrativeAuditRecorder auditRecorder,
    IUnitOfWork unitOfWork)
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
        if (activeProviders.Count == 0 && selectedProvider.IsActive)
        {
            return new ActivateProviderResult(ActivateProviderStatus.Success);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        foreach (var provider in activeProviders)
        {
            provider.SetActive(false);
            providerRepository.Update(provider);
            await RecordAsync(command, provider, AdministrativeAuditActions.Providers.Deactivated, cancellationToken);
        }

        if (activeProviders.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (!selectedProvider.IsActive)
        {
            selectedProvider.SetActive(true);
            providerRepository.Update(selectedProvider);
            await RecordAsync(command, selectedProvider, AdministrativeAuditActions.Providers.Activated, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new ActivateProviderResult(ActivateProviderStatus.Success);
    }

    private Task RecordAsync(
        ActivateProviderCommand command,
        Provider provider,
        string action,
        CancellationToken cancellationToken) =>
        auditRecorder.RecordAsync(
            new AdministrativeAuditEntry(
                command.ActorContext,
                action,
                AdministrativeAuditTargetTypes.Provider,
                provider.Id,
                provider.ProviderName,
                null,
                command.Reason,
                new AdministrativeAuditChanges().Field("isActive", !provider.IsActive, provider.IsActive)),
            cancellationToken);
}
