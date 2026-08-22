namespace BackendProjectTemplate.Domain.Common.Authentication;

public static class AuthenticationOtpDefaults
{
    public static readonly TimeSpan EmailConfirmationLifetime = TimeSpan.FromMinutes(10);
}
