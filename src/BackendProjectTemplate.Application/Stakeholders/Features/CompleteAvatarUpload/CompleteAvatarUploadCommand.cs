using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Stakeholders.Features.CompleteAvatarUpload;

public sealed record CompleteAvatarUploadCommand(Guid UploadId, ActorContext ActorContext);
