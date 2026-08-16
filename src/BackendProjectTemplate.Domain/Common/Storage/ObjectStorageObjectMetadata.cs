namespace BackendProjectTemplate.Domain.Common.Storage;

public sealed record ObjectStorageObjectMetadata(
    long ContentLength,
    string ContentType,
    string ETag);
