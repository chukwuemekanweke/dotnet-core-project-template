namespace BackendProjectTemplate.WebAPI.Features.Authentication.Security;

public sealed record TwoFactorSetupResponse(string SharedKey, string AuthenticatorUri);
