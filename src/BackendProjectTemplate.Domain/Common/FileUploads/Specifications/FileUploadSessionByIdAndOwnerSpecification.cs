using BackendProjectTemplate.Domain.Common.FileUploads.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Domain.Common.FileUploads.Specifications;

public sealed class FileUploadSessionByIdAndOwnerSpecification : Specification<FileUploadSession>
{
    public FileUploadSessionByIdAndOwnerSpecification(
        Guid uploadId,
        Guid tenantId,
        string ownerType,
        Guid ownerId,
        string purpose)
    {
        Where(upload =>
            upload.Id == uploadId &&
            upload.TenantId == tenantId &&
            upload.OwnerType == ownerType &&
            upload.OwnerId == ownerId &&
            upload.Purpose == purpose);
    }
}
