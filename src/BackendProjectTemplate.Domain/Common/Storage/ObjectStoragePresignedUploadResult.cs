namespace BackendProjectTemplate.Domain.Common.Storage;

public sealed record ObjectStoragePresignedUploadResult(
    string UploadUrl,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAtUtc);
