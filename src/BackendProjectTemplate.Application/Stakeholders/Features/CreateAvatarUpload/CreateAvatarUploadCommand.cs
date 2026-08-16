using BackendProjectTemplate.Domain.Common.Auditing;

namespace BackendProjectTemplate.Application.Stakeholders.Features.CreateAvatarUpload;

public sealed record CreateAvatarUploadCommand(
    string FileName,
    string ContentType,
    long ContentLength,
    ActorContext ActorContext);
