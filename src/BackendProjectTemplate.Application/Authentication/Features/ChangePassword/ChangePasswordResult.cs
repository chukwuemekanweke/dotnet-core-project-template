namespace BackendProjectTemplate.Application.Authentication.Features.ChangePassword;

public sealed record ChangePasswordResult(
    ChangePasswordStatus Status,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
