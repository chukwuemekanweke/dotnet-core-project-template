using BackendProjectTemplate.Domain.Authentication.Entities;

namespace BackendProjectTemplate.Application.Authentication;

public sealed record TwoFactorActor(AppUser User, Guid StakeholderId);
