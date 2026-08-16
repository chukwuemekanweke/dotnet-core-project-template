using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Common.Storage;
using BackendProjectTemplate.Domain.Notifications.Specifications;
using BackendProjectTemplate.Domain.Providers.Entities;

namespace BackendProjectTemplate.Infrastructure.Storage;

internal sealed class ObjectStorageService(
    IEnumerable<IObjectStorageProvider> providers,
    IReadRepository<Provider> providerRepository) : IObjectStorageService
{
    public async Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var provider = await ResolveProviderAsync(cancellationToken);
        return await provider.UploadPublicAsync(request, cancellationToken);
    }

    public async Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var provider = await ResolveProviderAsync(cancellationToken);
        return await provider.UploadPrivateAsync(request, cancellationToken);
    }

    public async Task<ObjectStoragePresignedUploadResult> CreatePrivatePresignedUploadAsync(
        ObjectStoragePresignedUploadRequest request,
        CancellationToken cancellationToken)
    {
        var provider = await ResolveProviderAsync(cancellationToken);
        return await provider.CreatePrivatePresignedUploadAsync(request, cancellationToken);
    }

    public async Task<ObjectStorageObjectMetadata?> GetPrivateObjectMetadataAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        var provider = await ResolveProviderAsync(cancellationToken);
        return await provider.GetPrivateObjectMetadataAsync(objectKey, cancellationToken);
    }

    public async Task<byte[]> ReadPrivateObjectRangeAsync(
        ObjectStorageRangeReadRequest request,
        CancellationToken cancellationToken)
    {
        var provider = await ResolveProviderAsync(cancellationToken);
        return await provider.ReadPrivateObjectRangeAsync(request, cancellationToken);
    }

    public async Task<string> PromotePrivateObjectAsync(
        ObjectStoragePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var provider = await ResolveProviderAsync(cancellationToken);
        return await provider.PromotePrivateObjectAsync(request, cancellationToken);
    }

    public async Task DeletePrivateObjectAsync(string objectKey, CancellationToken cancellationToken)
    {
        var provider = await ResolveProviderAsync(cancellationToken);
        await provider.DeletePrivateObjectAsync(objectKey, cancellationToken);
    }

    private async Task<IObjectStorageProvider> ResolveProviderAsync(CancellationToken cancellationToken)
    {
        var activeProvider = await providerRepository.FirstOrDefaultAsync(
            new ActiveProviderByTypeSpecification(ProviderType.FileStorage),
            cancellationToken);
        if (activeProvider is null)
        {
            throw new InvalidOperationException("No active file storage provider is configured.");
        }

        var provider = providers.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderKey, activeProvider.ProviderKey, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            throw new InvalidOperationException(
                $"No object storage provider is registered for key '{activeProvider.ProviderKey}'.");
        }

        return provider;
    }
}
