namespace BackendProjectTemplate.WebAPI.Features.Stakeholders.Profiles;

public sealed record CreateAvatarUploadResponse(
    Guid UploadId,
    string UploadUrl,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAtUtc);
