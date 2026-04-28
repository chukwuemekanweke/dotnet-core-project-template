namespace BackendProjectTemplate.Infrastructure.Payments.SafeHaven;

public sealed record SafeHavenVerificationResult(
    string Id,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    string? Bvn,
    string? Nin,
    bool Verified);
