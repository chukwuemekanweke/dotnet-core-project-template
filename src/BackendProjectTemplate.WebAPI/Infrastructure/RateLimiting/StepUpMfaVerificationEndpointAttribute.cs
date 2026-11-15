namespace BackendProjectTemplate.WebAPI.Infrastructure.RateLimiting;

[AttributeUsage(AttributeTargets.Method)]
public sealed class StepUpMfaVerificationEndpointAttribute : Attribute;
