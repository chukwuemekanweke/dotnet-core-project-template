namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record PasskeyRegistrationResult(PasskeyOperationStatus Status, PasskeyCredential? Passkey = null);
