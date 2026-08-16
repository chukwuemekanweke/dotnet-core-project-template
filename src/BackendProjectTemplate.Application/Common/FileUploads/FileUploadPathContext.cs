namespace BackendProjectTemplate.Application.Common.FileUploads;

public sealed record FileUploadPathContext(
    Guid UploadId,
    Guid TenantId,
    Guid OwnerId,
    string FileExtension);
