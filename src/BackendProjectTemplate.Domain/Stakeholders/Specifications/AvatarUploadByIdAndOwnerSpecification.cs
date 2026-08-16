using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Specifications;

public sealed class AvatarUploadByIdAndOwnerSpecification : Specification<AvatarUpload>
{
    public AvatarUploadByIdAndOwnerSpecification(Guid uploadId, Guid stakeholderId, Guid tenantId)
    {
        Where(upload =>
            upload.Id == uploadId &&
            upload.StakeholderId == stakeholderId &&
            upload.TenantId == tenantId);
        EnableTracking();
    }
}
