namespace BackendProjectTemplate.Domain.Common.Storage;

public sealed record ObjectStoragePromotionRequest(
    string SourceObjectKey,
    string DestinationObjectKey,
    string ExpectedSourceETag,
    string ContentType);
