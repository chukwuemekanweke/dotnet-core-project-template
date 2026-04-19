namespace BackendProjectTemplate.Application.Authentication.Features.RefreshSession;

public sealed record RefreshSessionCommand(
    string RefreshToken,
    string IpAddress,
    string UserAgent);
