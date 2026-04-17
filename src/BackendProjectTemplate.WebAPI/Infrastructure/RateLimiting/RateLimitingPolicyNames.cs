namespace BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;

public static class RateLimitingPolicyNames
{
    public const string AuthPublicPolicy = "auth-public-policy";
    public const string AuthenticatedUserPolicy = "authenticated-user-policy";
    public const string GlobalFallbackPolicy = "global-fallback-policy";
}
