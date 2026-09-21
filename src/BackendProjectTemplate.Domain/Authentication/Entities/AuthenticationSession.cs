using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Authentication.Entities;

public sealed class AuthenticationSession : Entity, IAggregateRoot
{
    private AuthenticationSession()
    {
    }

    private AuthenticationSession(Guid appUserId, Guid stakeholderId, Guid tenantId, Guid ipAddressId,
        string userAgent, string? deviceName, string? devicePlatform, string? browserName,
        DateTimeOffset now, DateTimeOffset expiresAtUtc)
    {
        AppUserId = appUserId;
        StakeholderId = stakeholderId;
        TenantId = tenantId;
        FirstIpAddressId = ipAddressId;
        LastIpAddressId = ipAddressId;
        UserAgent = userAgent;
        DeviceName = deviceName;
        DevicePlatform = devicePlatform;
        BrowserName = browserName;
        LastActiveAtUtc = now;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid AppUserId { get; private set; }
    public Guid StakeholderId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTimeOffset LastActiveAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string? DeviceName { get; private set; }
    public string? DevicePlatform { get; private set; }
    public string? BrowserName { get; private set; }
    public Guid FirstIpAddressId { get; private set; }
    public Guid LastIpAddressId { get; private set; }
    public AppUser AppUser { get; private set; } = null!;
    public IpAddress FirstIpAddress { get; private set; } = null!;
    public IpAddress LastIpAddress { get; private set; } = null!;

    public static AuthenticationSession Create(Guid appUserId, Guid stakeholderId, Guid tenantId,
        Guid ipAddressId, string userAgent, string? deviceName, string? devicePlatform,
        string? browserName, DateTimeOffset now, DateTimeOffset expiresAtUtc) =>
        new(appUserId, stakeholderId, tenantId, ipAddressId, userAgent, deviceName,
            devicePlatform, browserName, now, expiresAtUtc);

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;

    public void Touch(DateTimeOffset now, Guid ipAddressId, string userAgent,
        string? deviceName, string? devicePlatform, string? browserName)
    {
        if (!IsActive(now))
        {
            return;
        }
        LastActiveAtUtc = now;
        LastIpAddressId = ipAddressId;
        UserAgent = userAgent;
        DeviceName = deviceName;
        DevicePlatform = devicePlatform;
        BrowserName = browserName;
    }

    public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now;
}
