namespace BackendProjectTemplate.Application.Common.FileUploads;

public sealed record FileUploadPreparationRequest(
    Guid TenantId,
    Guid OwnerId,
    string FileName,
    string ContentType,
    long ContentLength);
