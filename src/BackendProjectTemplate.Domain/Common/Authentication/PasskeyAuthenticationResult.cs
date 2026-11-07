using BackendProjectTemplate.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Domain.Common.Authentication;

public sealed record PasskeyAuthenticationResult(
    PasskeyOperationStatus Status,
    AppUser? User = null,
    UserPasskeyInfo? Passkey = null);
