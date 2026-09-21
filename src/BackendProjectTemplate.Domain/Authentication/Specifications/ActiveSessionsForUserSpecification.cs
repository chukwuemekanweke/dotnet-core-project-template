using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Common.Persistence;

namespace BackendProjectTemplate.Domain.Authentication.Specifications;

public sealed class ActiveSessionsForUserSpecification : Specification<AuthenticationSession>
{
    public ActiveSessionsForUserSpecification(Guid appUserId, DateTimeOffset now)
    {
        Where(session => session.AppUserId == appUserId && session.RevokedAtUtc == null && session.ExpiresAtUtc > now);
        AddInclude(session => session.FirstIpAddress);
        AddInclude(session => session.LastIpAddress);
        EnableTracking();
    }
}
