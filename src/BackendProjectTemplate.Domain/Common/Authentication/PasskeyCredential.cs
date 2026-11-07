namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record PasskeyCredential(
    string Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> Transports,
    bool IsBackupEligible,
    bool IsBackedUp);
