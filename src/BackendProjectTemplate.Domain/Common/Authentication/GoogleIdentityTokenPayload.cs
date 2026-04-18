namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record GoogleIdentityTokenPayload(
    string Subject,
    string Email,
    string? DisplayName);
