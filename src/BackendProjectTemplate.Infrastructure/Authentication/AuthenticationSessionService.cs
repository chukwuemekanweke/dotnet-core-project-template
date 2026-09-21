using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Authentication.Services;
using BackendProjectTemplate.Domain.Authentication.Specifications;
using BackendProjectTemplate.Domain.Common.Persistence;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Infrastructure.Authentication;

public sealed class AuthenticationSessionService(
    IRepository<AuthenticationSession> repository,
    IIpAddressResolver ipAddressResolver,
    IUserAgentParserService userAgentParser,
    TimeProvider timeProvider) : IAuthenticationSessionService
{
    public async Task<AuthenticationSession> CreateAsync(AppUser user, Stakeholder stakeholder,
        string ipAddress, string userAgent, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken)
    {
        var ip = await ipAddressResolver.ResolveAsync(ipAddress, cancellationToken);
        var device = userAgentParser.Parse(userAgent);
        var session = AuthenticationSession.Create(user.Id, stakeholder.Id, stakeholder.TenantId,
            ip.IpAddressId, userAgent, device.DeviceName, device.DevicePlatform, device.BrowserName,
            timeProvider.GetUtcNow(), expiresAtUtc);
        await repository.AddAsync(session, cancellationToken);
        return session;
    }

    public Task<AuthenticationSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(sessionId, cancellationToken);

    public Task<IReadOnlyList<AuthenticationSession>> ListActiveAsync(Guid appUserId, CancellationToken cancellationToken) =>
        repository.ListAsync(new ActiveSessionsForUserSpecification(appUserId, timeProvider.GetUtcNow()), cancellationToken);

    public async Task<bool> RevokeAsync(Guid sessionId, Guid appUserId, CancellationToken cancellationToken)
    {
        var session = await FindAsync(sessionId, cancellationToken);
        if (session is null || session.AppUserId != appUserId) return false;
        session.Revoke(timeProvider.GetUtcNow());
        repository.Update(session);
        return true;
    }

    public async Task RevokeOthersAsync(Guid currentSessionId, Guid appUserId, Guid stakeholderId, Guid tenantId, CancellationToken cancellationToken)
    {
        foreach (var session in await ListActiveAsync(appUserId, cancellationToken))
        {
            if (session.Id != currentSessionId && session.StakeholderId == stakeholderId && session.TenantId == tenantId)
            {
                session.Revoke(timeProvider.GetUtcNow());
                repository.Update(session);
            }
        }
    }

    public async Task RevokeAllAsync(Guid appUserId, CancellationToken cancellationToken)
    {
        foreach (var session in await ListActiveAsync(appUserId, cancellationToken))
        {
            session.Revoke(timeProvider.GetUtcNow());
            repository.Update(session);
        }
    }

    public async Task TouchAsync(AuthenticationSession session, string ipAddress, string userAgent,
        CancellationToken cancellationToken)
    {
        var ip = await ipAddressResolver.ResolveAsync(ipAddress, cancellationToken);
        var device = userAgentParser.Parse(userAgent);
        session.Touch(timeProvider.GetUtcNow(), ip.IpAddressId, userAgent,
            device.DeviceName, device.DevicePlatform, device.BrowserName);
        repository.Update(session);
    }
}
