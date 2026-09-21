using BackendProjectTemplate.Domain.Authentication.Entities;
using BackendProjectTemplate.Domain.Stakeholders.Entities;

namespace BackendProjectTemplate.Domain.Authentication.Services;

public interface IAuthenticationSessionService
{
    Task<AuthenticationSession> CreateAsync(AppUser user, Stakeholder stakeholder, string ipAddress,
        string userAgent, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
    Task<AuthenticationSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuthenticationSession>> ListActiveAsync(Guid appUserId, CancellationToken cancellationToken);
    Task<bool> RevokeAsync(Guid sessionId, Guid appUserId, CancellationToken cancellationToken);
    Task RevokeOthersAsync(Guid currentSessionId, Guid appUserId, Guid stakeholderId, Guid tenantId, CancellationToken cancellationToken);
    Task RevokeAllAsync(Guid appUserId, CancellationToken cancellationToken);
    Task TouchAsync(AuthenticationSession session, string ipAddress, string userAgent, CancellationToken cancellationToken);
}
