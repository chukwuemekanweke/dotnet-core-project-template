using BackendProjectTemplate.Contracts.Common;
using BackendProjectTemplate.Domain.Common.Auditing;
using Microsoft.AspNetCore.Identity;

namespace BackendProjectTemplate.Domain.Authentication.Entities;

public sealed class AppUser : IdentityUser<Guid>, IAuditableEntity, ISoftDelete
{
    private AppUser()
    {
    }

    private AppUser(string email, DateTimeOffset utcNow)
    {
        var normalizedEmail = email.Trim();

        UserName = normalizedEmail;
        Email = normalizedEmail;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }
    public ICollection<AuthenticationRefreshToken> RefreshTokens { get; private set; } = [];

    public static AppUser Create(string email, DateTimeOffset utcNow) =>
        new(email, utcNow);

    public static AppUser Create(string email, string firstName, string lastName, DateTimeOffset utcNow) =>
        new(email, utcNow);

    public void MarkEmailVerified(DateTimeOffset utcNow)
    {
        EmailConfirmed = true;
        UpdatedAtUtc = utcNow;
    }

    public void Touch(DateTimeOffset utcNow) => UpdatedAtUtc = utcNow;

    public void SetCreatedAudit(DateTimeOffset utcNow, string actorId)
    {
        CreatedAtUtc = utcNow;
        CreatedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }

    public void SetUpdatedAudit(DateTimeOffset utcNow, string actorId)
    {
        UpdatedAtUtc = utcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }

    public void SetDeletedAudit(DateTimeOffset utcNow, string actorId)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }
}
