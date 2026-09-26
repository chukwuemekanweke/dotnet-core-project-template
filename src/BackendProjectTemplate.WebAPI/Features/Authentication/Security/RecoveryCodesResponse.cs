namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

public sealed record RecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
