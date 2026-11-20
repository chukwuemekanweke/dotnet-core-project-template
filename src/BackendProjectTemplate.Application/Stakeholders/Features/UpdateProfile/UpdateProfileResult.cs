namespace BackendProjectTemplate.Application.Stakeholders.Features.UpdateProfile;

public sealed record UpdateProfileResult(
    UpdateProfileStatus Status,
    string? Error = null);
