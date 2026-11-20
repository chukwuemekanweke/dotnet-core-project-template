namespace BackendProjectTemplate.Application.Authentication.Features.CompletePasswordReset;

public sealed record CompletePasswordResetResult(
    CompletePasswordResetStatus Status,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
