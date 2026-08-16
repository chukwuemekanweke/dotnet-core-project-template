using BackendProjectTemplate.Domain.Common.Storage;
using System.Collections.Concurrent;

namespace BackendProjectTemplate.WebAPI.IntegrationTests.Infrastructure;

public sealed class FakeObjectStorageService : IObjectStorageService
{
    private readonly ConcurrentDictionary<string, StoredObject> _privateObjects = new(StringComparer.Ordinal);

    public Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(BuildPublicUrl(request.ObjectKey));

    public Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken) =>
        Task.FromResult($"https://storage.integration.invalid/private/{request.ObjectKey}");

    public Task<ObjectStoragePresignedUploadResult> CreatePrivatePresignedUploadAsync(
        ObjectStoragePresignedUploadRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ObjectStoragePresignedUploadResult(
            $"https://storage.integration.invalid/private/{request.ObjectKey}?signature=test",
            new Dictionary<string, string> { ["Content-Type"] = request.ContentType },
            request.ExpiresAtUtc));

    public Task<ObjectStorageObjectMetadata?> GetPrivateObjectMetadataAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        var metadata = _privateObjects.TryGetValue(objectKey, out var storedObject)
            ? new ObjectStorageObjectMetadata(storedObject.Content.LongLength, storedObject.ContentType, storedObject.ETag)
            : null;
        return Task.FromResult(metadata);
    }

    public Task<byte[]> ReadPrivateObjectRangeAsync(
        ObjectStorageRangeReadRequest request,
        CancellationToken cancellationToken)
    {
        var storedObject = GetMatchingObject(request.ObjectKey, request.ExpectedETag);
        var start = checked((int)request.Start);
        var length = Math.Min(checked((int)(request.End - request.Start + 1)), storedObject.Content.Length - start);
        return Task.FromResult(storedObject.Content.AsSpan(start, length).ToArray());
    }

    public Task<string> PromotePrivateObjectToPublicAsync(
        ObjectStoragePromotionRequest request,
        CancellationToken cancellationToken)
    {
        _ = GetMatchingObject(request.SourceObjectKey, request.ExpectedSourceETag);
        return Task.FromResult(BuildPublicUrl(request.DestinationObjectKey));
    }

    public Task DeletePrivateObjectAsync(string objectKey, CancellationToken cancellationToken)
    {
        _privateObjects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }

    public void StorePrivateObject(string objectKey, byte[] content, string contentType)
    {
        _privateObjects[objectKey] = new StoredObject(
            content.ToArray(),
            contentType,
            $"\"{Guid.CreateVersion7():N}\"");
    }

    private StoredObject GetMatchingObject(string objectKey, string expectedETag)
    {
        if (!_privateObjects.TryGetValue(objectKey, out var storedObject) || storedObject.ETag != expectedETag)
        {
            throw new ObjectStoragePreconditionFailedException("The object changed.");
        }

        return storedObject;
    }

    private static string BuildPublicUrl(string objectKey) =>
        $"https://cdn.integration.invalid/{objectKey}";

    private sealed record StoredObject(byte[] Content, string ContentType, string ETag);
}
