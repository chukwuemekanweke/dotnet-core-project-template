using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Stakeholders.Entities;

public sealed class AppUserStakeholder : Entity
{
    private AppUserStakeholder()
    {
    }

    private AppUserStakeholder(Guid appUserId, Guid stakeholderId, DateTimeOffset utcNow)
    {
        AppUserId = appUserId;
        StakeholderId = stakeholderId;
    }

    public Guid AppUserId { get; private set; }
    public Guid StakeholderId { get; private set; }

    public AppUser AppUser { get; private set; } = null!;
    public Stakeholder Stakeholder { get; private set; } = null!;

    public static AppUserStakeholder Create(Guid appUserId, Guid stakeholderId, DateTimeOffset utcNow) =>
        new(appUserId, stakeholderId, utcNow);
}
