namespace BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;

using BackendProjectTemplate.Domain.Common.Auditing;

public sealed record UploadAvatarCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long ContentLength,
    ActorContext ActorContext);
