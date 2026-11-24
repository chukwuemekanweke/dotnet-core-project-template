using BackendProjectTemplate.Domain.Common.Entities;

namespace BackendProjectTemplate.Domain.Authentication.Entities;

public sealed class SecurityActivity : Entity, IAggregateRoot
{
    public const int MaxFailureReasonLength = 100;
    private const int MaxUserAgentLength = 500;
    private const int MaxDeviceNameLength = 200;
    private const int MaxDevicePlatformLength = 100;
    private const int MaxBrowserNameLength = 100;

    private SecurityActivity()
    {
    }

    private SecurityActivity(
        Guid stakeholderId,
        Guid tenantId,
        Guid ipAddressId,
        Guid? ipAddressLocationId,
        string userAgent,
        string? deviceName,
        string? devicePlatform,
        string? browserName,
        DateTimeOffset occurredAtUtc,
        SecurityActivityType activityType,
        string? failureReason = null)
    {
        StakeholderId = stakeholderId;
        TenantId = tenantId;
        IpAddressId = ipAddressId;
        IpAddressLocationId = ipAddressLocationId;
        UserAgent = NormalizeUserAgent(userAgent);
        DeviceName = NormalizeOptional(deviceName, MaxDeviceNameLength);
        DevicePlatform = NormalizeOptional(devicePlatform, MaxDevicePlatformLength);
        BrowserName = NormalizeOptional(browserName, MaxBrowserNameLength);
        OccurredAtUtc = occurredAtUtc;
        ActivityType = activityType;
        FailureReason = NormalizeOptional(failureReason, MaxFailureReasonLength);
    }

    public Guid StakeholderId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid IpAddressId { get; private set; }
    public IpAddress? IpAddress { get; private set; }
    public Guid? IpAddressLocationId { get; private set; }
    public IpAddressLocation? IpAddressLocation { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string? DeviceName { get; private set; }
    public string? DevicePlatform { get; private set; }
    public string? BrowserName { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public SecurityActivityType ActivityType { get; private set; }
    public string? FailureReason { get; private set; }

    public static SecurityActivity CreateInitialLogin(
        Guid stakeholderId,
        Guid tenantId,
        Guid ipAddressId,
        Guid? ipAddressLocationId,
        string userAgent,
        string? deviceName,
        string? devicePlatform,
        string? browserName,
        DateTimeOffset occurredAtUtc) =>
        new(
            stakeholderId,
            tenantId,
            ipAddressId,
            ipAddressLocationId,
            userAgent,
            deviceName,
            devicePlatform,
            browserName,
            occurredAtUtc,
            SecurityActivityType.InitialLogin);

    public static SecurityActivity CreateTokenRefresh(
        Guid stakeholderId,
        Guid tenantId,
        Guid ipAddressId,
        Guid? ipAddressLocationId,
        string userAgent,
        string? deviceName,
        string? devicePlatform,
        string? browserName,
        DateTimeOffset occurredAtUtc) =>
        new(
            stakeholderId,
            tenantId,
            ipAddressId,
            ipAddressLocationId,
            userAgent,
            deviceName,
            devicePlatform,
            browserName,
            occurredAtUtc,
            SecurityActivityType.TokenRefresh);

    public static SecurityActivity CreateFailedSignIn(
        Guid stakeholderId,
        Guid tenantId,
        Guid ipAddressId,
        Guid? ipAddressLocationId,
        string userAgent,
        string? deviceName,
        string? devicePlatform,
        string? browserName,
        DateTimeOffset occurredAtUtc,
        string failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(failureReason));
        }

        return new(
            stakeholderId,
            tenantId,
            ipAddressId,
            ipAddressLocationId,
            userAgent,
            deviceName,
            devicePlatform,
            browserName,
            occurredAtUtc,
            SecurityActivityType.FailedSignIn,
            failureReason);
    }

    public static SecurityActivity CreateAccountSecurityChange(
        SecurityActivityType activityType,
        Guid stakeholderId,
        Guid tenantId,
        Guid ipAddressId,
        Guid? ipAddressLocationId,
        string userAgent,
        string? deviceName,
        string? devicePlatform,
        string? browserName,
        DateTimeOffset occurredAtUtc)
    {
        if (!IsAccountSecurityChange(activityType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(activityType),
                activityType,
                "Activity type is not an account security change.");
        }

        return new(
            stakeholderId,
            tenantId,
            ipAddressId,
            ipAddressLocationId,
            userAgent,
            deviceName,
            devicePlatform,
            browserName,
            occurredAtUtc,
            activityType);
    }

    public static bool IsAccountSecurityChange(SecurityActivityType activityType) => activityType is
        SecurityActivityType.PasskeyAdded or
        SecurityActivityType.PasskeyRemoved or
        SecurityActivityType.TwoFactorEnabled or
        SecurityActivityType.TwoFactorDisabled or
        SecurityActivityType.TwoFactorRecoveryCodesRegenerated or
        SecurityActivityType.PasswordChanged;

    private static string NormalizeUserAgent(string userAgent)
    {
        var normalized = userAgent.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("User agent is required.", nameof(userAgent));
        }

        if (normalized.Length > MaxUserAgentLength)
        {
            throw new ArgumentException($"User agent must not exceed {MaxUserAgentLength} characters.", nameof(userAgent));
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", nameof(value));
        }

        return normalized;
    }
}
