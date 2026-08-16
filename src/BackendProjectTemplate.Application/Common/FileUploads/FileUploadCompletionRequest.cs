using BackendProjectTemplate.Domain.Common.Storage;

namespace BackendProjectTemplate.Application.Common.FileUploads;

public sealed record FileUploadCompletionRequest(
    string QuarantineObjectKey,
    string FinalObjectKey,
    string ExpectedContentType,
    long ExpectedContentLength,
    ObjectStorageVisibility DestinationVisibility);
