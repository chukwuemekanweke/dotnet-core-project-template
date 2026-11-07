namespace BackendProjectTemplate.WebAPI.Features.Authentication.Passkeys;

public sealed record PasskeyResponse(
    string Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> Transports,
    bool IsBackupEligible,
    bool IsBackedUp);
