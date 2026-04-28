namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

internal sealed record SafeHavenCreateSubAccountPayload(
    string PhoneNumber,
    string Email,
    string ExternalReference,
    string IdentityType,
    string? IdentityNumber,
    string? IdentityId,
    string? Otp,
    string CallbackUrl,
    bool AutoSweep,
    SafeHavenCreateSubAccountAutoSweepDetailsPayload AutoSweepDetails);
