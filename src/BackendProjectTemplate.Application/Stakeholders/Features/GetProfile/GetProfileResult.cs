namespace BackendProjectTemplate.Application.Stakeholders.Features.GetProfile;

public sealed record GetProfileResult(
    GetProfileStatus Status,
    GetProfileResponse? Profile = null);
