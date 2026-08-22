namespace BackendProjectTemplate.Domain.Common.Authentication;

public static class AuthenticationOtpDefaults
{
    public static readonly TimeSpan EmailConfirmationLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan EmailConfirmationSessionLifetime = TimeSpan.FromDays(1);
}
