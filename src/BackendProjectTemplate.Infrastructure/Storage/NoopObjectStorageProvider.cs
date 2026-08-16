using BackendProjectTemplate.Domain.Common.Storage;

namespace BackendProjectTemplate.Infrastructure.Storage;

internal sealed class NoopObjectStorageProvider : IObjectStorageProvider
{
    public string ProviderKey => ObjectStorageProviderKeys.Noop;

    public Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(BuildPublicUrl(request.ObjectKey));

    public Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken) =>
        Task.FromResult($"https://example.invalid/private/{NormalizeObjectKey(request.ObjectKey)}");

    public Task<ObjectStoragePresignedUploadResult> CreatePrivatePresignedUploadAsync(
        ObjectStoragePresignedUploadRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ObjectStoragePresignedUploadResult(
            $"https://example.invalid/private/{NormalizeObjectKey(request.ObjectKey)}?signature=noop",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = request.ContentType
            },
            request.ExpiresAtUtc));

    public Task<ObjectStorageObjectMetadata?> GetPrivateObjectMetadataAsync(
        string objectKey,
        CancellationToken cancellationToken) => Task.FromResult<ObjectStorageObjectMetadata?>(null);

    public Task<byte[]> ReadPrivateObjectRangeAsync(
        ObjectStorageRangeReadRequest request,
        CancellationToken cancellationToken) => Task.FromResult(Array.Empty<byte>());

    public Task<string> PromotePrivateObjectToPublicAsync(
        ObjectStoragePromotionRequest request,
        CancellationToken cancellationToken) => Task.FromResult(BuildPublicUrl(request.DestinationObjectKey));

    public Task DeletePrivateObjectAsync(string objectKey, CancellationToken cancellationToken) => Task.CompletedTask;

    private static string BuildPublicUrl(string objectKey) =>
        $"https://example.invalid/{NormalizeObjectKey(objectKey)}";

    private static string NormalizeObjectKey(string objectKey) =>
        objectKey.TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);
}
