namespace BackendProjectTemplate.Application.Authentication.Features.LogoutSession;

public sealed record LogoutSessionCommand(
    string TokenId,
    DateTimeOffset ExpiresAtUtc,
    Guid? StakeholderId);
