namespace BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;

public sealed record CreateAvatarUploadRequest(string FileName, string ContentType, long ContentLength);
