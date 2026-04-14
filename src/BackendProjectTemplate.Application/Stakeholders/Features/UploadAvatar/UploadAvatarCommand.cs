namespace BackendProjectTemplate.Application.Stakeholders.Features.UploadAvatar;

public sealed record UploadAvatarCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long ContentLength);
