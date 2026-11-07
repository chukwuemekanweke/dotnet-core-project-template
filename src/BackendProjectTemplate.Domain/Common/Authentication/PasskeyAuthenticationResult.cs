using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record PasskeyAuthenticationResult(PasskeyOperationStatus Status, AppUser? User = null);
