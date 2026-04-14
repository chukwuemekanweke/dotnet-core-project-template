using BackendProjectTemplate.Domain.Common.Storage;

namespace BackendProjectTemplate.Infrastructure.Storage;

internal interface IObjectStorageProvider
{
    string ProviderKey { get; }

    Task<string> UploadPublicAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
    Task<string> UploadPrivateAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);
}
