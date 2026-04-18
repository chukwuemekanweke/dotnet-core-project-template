namespace BackendProjectTemplate.Application.Authentication.Features.GoogleSignIn;

public sealed record GoogleSignInCommand(
    string IdToken,
    string IpAddress,
    string UserAgent);
